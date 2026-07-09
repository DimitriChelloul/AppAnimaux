namespace Shared.Config;

public sealed class ServiceEndpointOptions
{
    public required string Name { get; init; }
    public required Uri BaseUrl { get; init; }
}