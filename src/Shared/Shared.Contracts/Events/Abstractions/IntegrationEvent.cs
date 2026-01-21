using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Abstractions
{
    public abstract record IntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

        // Traçabilité distribuée
        public Guid? CorrelationId { get; init; }
        public Guid? CausationId { get; init; }

        // Source
        public string SourceService { get; init; } = default!;
    }

}
