using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Outbox;

public sealed record OutboxRow
{
    public long Id { get; init; }
    public Guid MessageId { get; init; }

    public string? AggregateType { get; init; }
    public Guid? AggregateId { get; init; }

    public string Type { get; init; } = default!;
    public string Payload { get; init; } = default!; // JSON string (payload::text)

    public DateTimeOffset OccurredOn { get; init; }

    public string Status { get; init; } = "pending";
    public DateTimeOffset? ProcessedOn { get; init; }
    public string? Error { get; init; }
}


