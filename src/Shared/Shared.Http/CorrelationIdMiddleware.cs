using Microsoft.AspNetCore.Http;

namespace Shared.Http;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationId.HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : CorrelationId.Create();

        context.Items[CorrelationId.HeaderName] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;
        await _next(context);
    }
}