using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Abstractions
{
    public record EventEnvelope<T>(string Type, int Version, T Data, DateTimeOffset OccurredOn, Guid MessageId) where T : IntegrationEvent;

}
