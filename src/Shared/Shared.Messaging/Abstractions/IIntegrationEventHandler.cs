using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Abstractions;

public interface IIntegrationEventHandler<in TEvent>
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}

