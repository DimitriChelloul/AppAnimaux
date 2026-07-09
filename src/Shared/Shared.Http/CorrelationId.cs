namespace Shared.Http;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";

    public static string Create() => Guid.CreateVersion7().ToString("N");
}