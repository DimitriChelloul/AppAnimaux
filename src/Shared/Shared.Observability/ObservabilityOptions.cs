namespace Shared.Observability;

public sealed class ObservabilityOptions
{
    public string ServiceName { get; init; } = AppDomain.CurrentDomain.FriendlyName;
    public bool EnableHttpLogging { get; init; }
}