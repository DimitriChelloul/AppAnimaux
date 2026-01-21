using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Abstractions;


public interface IEventPublisher
{
    Task PublishAsync(
        string exchange,
        string routingKey,
        ReadOnlyMemory<byte> body,
        IDictionary<string, object>? headers = null,
        CancellationToken ct = default);
}


