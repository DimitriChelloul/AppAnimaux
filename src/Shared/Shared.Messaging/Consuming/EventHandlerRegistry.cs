using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Consuming;

public interface IEventHandlerRegistry
{
    void Register(string eventType, Func<string, CancellationToken, Task> handleJsonAsync);
    bool TryGet(string eventType, out Func<string, CancellationToken, Task> handler);
}

public sealed class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers = new(StringComparer.Ordinal);

    public void Register(string eventType, Func<string, CancellationToken, Task> handleJsonAsync)
        => _handlers[eventType] = handleJsonAsync;

    public bool TryGet(string eventType, out Func<string, CancellationToken, Task> handler)
        => _handlers.TryGetValue(eventType, out handler!);
}

