namespace Shared.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    // Connexion
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";

    // (optionnel) SSL - pas utilisé dans le mini code, mais OK à garder
    public bool UseSsl { get; init; } = false;

    // Exchange commun
    public string ExchangeName { get; init; } = "appanimaux.events";

    // Consumer (si le service consomme)
    public string QueueName { get; init; } = "service.events";
    public string[] Bindings { get; init; } = Array.Empty<string>();
    public ushort PrefetchCount { get; init; } = 20;
    public int MaxPublishAttempts { get; init; } = 10;
    public int RetryBaseDelaySeconds { get; init; } = 5;
    public int MaintenanceIntervalMinutes { get; init; } = 15;
    public int ProcessedOutboxRetentionDays { get; init; } = 7;
    public int FailedOutboxRetentionDays { get; init; } = 30;
    public int InboxRetentionDays { get; init; } = 30;
    public string DeadLetterExchangeName => $"{ExchangeName}.dead-letter";
    public string DeadLetterQueueName => $"{QueueName}.dead-letter";
}
