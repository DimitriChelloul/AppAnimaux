using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Shared.Messaging.Outbox;
using Shared.Persistence.Postgres;
using Shared.Persistence.Transactions;
using Testcontainers.PostgreSql;

namespace Shared.Messaging.Tests;

public sealed class TransactionalOutboxIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("appanimaux_test")
        .WithUsername("app")
        .WithPassword("app-test-password")
        .Build();

    private NpgsqlConnectionFactory _factory = null!;
    private OutboxRepository _outbox = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new NpgsqlConnectionFactory(Options.Create(new PostgresOptions { ConnectionString = _postgres.GetConnectionString() }));
        _outbox = new OutboxRepository(_factory);
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.ExecuteAsync("""
            CREATE TABLE business_records(id uuid PRIMARY KEY, value text NOT NULL);
            CREATE TABLE outbox_messages(
                id bigserial PRIMARY KEY, message_id uuid NOT NULL UNIQUE,
                aggregate_type varchar(100), aggregate_id uuid, type varchar(200) NOT NULL,
                payload jsonb NOT NULL, occurred_on timestamptz NOT NULL,
                processed_on timestamptz, status varchar(20) NOT NULL DEFAULT 'pending',
                error text, attempts integer NOT NULL DEFAULT 0, next_attempt_on timestamptz);
            CREATE TABLE inbox_messages(
                message_id uuid PRIMARY KEY, event_type varchar(200) NOT NULL,
                processed_on timestamptz NOT NULL DEFAULT now());
            """);
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Business_write_and_outbox_commit_together()
    {
        var aggregateId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        await TransactionalOutbox.ExecuteAsync(async () =>
        {
            using (var connection = _factory.Create()) {
                connection.Open();
                await connection.ExecuteAsync("INSERT INTO business_records(id, value) VALUES (@Id, 'committed')", new { Id = aggregateId });
            }
            await _outbox.AddAsync(messageId, "User.Registered", "{}", "user", aggregateId);
        });

        await using var verification = new NpgsqlConnection(_postgres.GetConnectionString());
        Assert.Equal(1, await verification.ExecuteScalarAsync<int>("SELECT count(*) FROM business_records WHERE id=@Id", new { Id = aggregateId }));
        Assert.Equal("pending", await verification.ExecuteScalarAsync<string>("SELECT status FROM outbox_messages WHERE message_id=@Id", new { Id = messageId }));
    }

    [Fact]
    public async Task Failure_rolls_back_business_write_and_outbox()
    {
        var aggregateId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => TransactionalOutbox.ExecuteAsync(async () =>
        {
            using (var connection = _factory.Create()) {
                connection.Open();
                await connection.ExecuteAsync("INSERT INTO business_records(id, value) VALUES (@Id, 'rolled-back')", new { Id = aggregateId });
            }
            await _outbox.AddAsync(messageId, "User.Registered", "{}", "user", aggregateId);
            throw new InvalidOperationException("failure before commit");
        }));

        await using var verification = new NpgsqlConnection(_postgres.GetConnectionString());
        Assert.Equal(0, await verification.ExecuteScalarAsync<int>("SELECT count(*) FROM business_records WHERE id=@Id", new { Id = aggregateId }));
        Assert.Equal(0, await verification.ExecuteScalarAsync<int>("SELECT count(*) FROM outbox_messages WHERE message_id=@Id", new { Id = messageId }));
    }

    [Fact]
    public async Task Inbox_primary_key_makes_redelivery_idempotent()
    {
        var messageId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        const string sql = "INSERT INTO inbox_messages(message_id, event_type) VALUES (@MessageId, 'User.Registered') ON CONFLICT (message_id) DO NOTHING";
        Assert.Equal(1, await connection.ExecuteAsync(sql, new { MessageId = messageId }));
        Assert.Equal(0, await connection.ExecuteAsync(sql, new { MessageId = messageId }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM inbox_messages WHERE message_id=@MessageId", new { MessageId = messageId }));
    }

    [Fact]
    public async Task Maintenance_purges_only_expired_rows_and_reports_backlog()
    {
        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.ExecuteAsync("""
                INSERT INTO outbox_messages(message_id, type, payload, occurred_on, processed_on, status)
                VALUES
                  (@OldProcessed, 'User.Registered', '{}', now() - interval '40 days', now() - interval '40 days', 'processed'),
                  (@RecentProcessed, 'User.Registered', '{}', now(), now(), 'processed'),
                  (@OldFailed, 'User.Registered', '{}', now() - interval '40 days', NULL, 'failed'),
                  (@RecentFailed, 'User.Registered', '{}', now(), NULL, 'failed'),
                  (@Pending, 'User.Registered', '{}', now() - interval '2 hours', NULL, 'pending');
                INSERT INTO inbox_messages(message_id, event_type, processed_on)
                VALUES (@OldInbox, 'User.Registered', now() - interval '40 days');
                """, new
            {
                OldProcessed = Guid.NewGuid(),
                RecentProcessed = Guid.NewGuid(),
                OldFailed = Guid.NewGuid(),
                RecentFailed = Guid.NewGuid(),
                Pending = Guid.NewGuid(),
                OldInbox = Guid.NewGuid()
            });
        }

        var maintenance = new MessagingMaintenance(_factory, Options.Create(new Shared.Messaging.RabbitMq.RabbitMqOptions
        {
            ProcessedOutboxRetentionDays = 7,
            FailedOutboxRetentionDays = 30,
            InboxRetentionDays = 30
        }));

        var snapshot = await maintenance.RunOnceAsync();

        Assert.Equal(1, snapshot.DeletedProcessed);
        Assert.Equal(1, snapshot.DeletedFailed);
        Assert.Equal(1, snapshot.DeletedInbox);
        Assert.Equal(1, snapshot.PendingCount);
        Assert.Equal(1, snapshot.FailedCount);
        Assert.NotNull(snapshot.OldestPendingOn);
    }
    [Fact]
    public async Task Two_publishers_do_not_poll_the_same_outbox_row_concurrently()
    {
        var messageId = Guid.NewGuid();
        await _outbox.AddAsync(messageId, Shared.Contracts.Messaging.EventTypes.Users.UserRegistered, "{}", "user", Guid.NewGuid());

        var publisher = new CountingPublisher();
        var options = Options.Create(new Shared.Messaging.RabbitMq.RabbitMqOptions());
        var first = new OutboxPublisherHostedService(
            _factory, publisher, new Shared.Messaging.Routing.DefaultEventRoutingMapper(), options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxPublisherHostedService>.Instance);
        var second = new OutboxPublisherHostedService(
            _factory, publisher, new Shared.Messaging.Routing.DefaultEventRoutingMapper(), options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxPublisherHostedService>.Instance);

        await Task.WhenAll(
            first.PublishBatchAsync(CancellationToken.None),
            second.PublishBatchAsync(CancellationToken.None));

        Assert.Equal(1, publisher.CallCount);
        await using var verification = new NpgsqlConnection(_postgres.GetConnectionString());
        Assert.Equal("processed", await verification.ExecuteScalarAsync<string>(
            "SELECT status FROM outbox_messages WHERE message_id=@MessageId", new { MessageId = messageId }));
    }

    private sealed class CountingPublisher : Shared.Messaging.Abstractions.IEventPublisher
    {
        private int _callCount;
        public int CallCount => _callCount;

        public async Task PublishAsync(
            string exchange,
            string routingKey,
            ReadOnlyMemory<byte> body,
            IDictionary<string, object>? headers = null,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            await Task.Delay(250, ct);
        }
    }}
