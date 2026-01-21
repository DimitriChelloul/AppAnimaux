namespace Shared.Messaging.Routing;

public interface IEventRoutingMapper
{
    string GetRoutingKey(string eventType);
}
