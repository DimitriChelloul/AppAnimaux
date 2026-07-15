using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Shared.Messaging.Outbox;

public sealed class GenericMutationOutboxMiddleware
{
    private static readonly HashSet<string> MutationMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;
    private readonly string _serviceName;

    public GenericMutationOutboxMiddleware(RequestDelegate next, string serviceName)
    {
        _next = next;
        _serviceName = serviceName;
    }

    public async Task InvokeAsync(HttpContext context, IOutboxRepository outbox)
    {
        await _next(context);

        if (!MutationMethods.Contains(context.Request.Method) ||
            context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return;
        }

        var messageId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;
        var eventType = $"{_serviceName}.MutationCompleted";
        var payload = JsonSerializer.Serialize(new
        {
            type = eventType,
            version = 1,
            data = new
            {
                eventId = messageId,
                occurredOn,
                sourceService = _serviceName,
                method = context.Request.Method,
                path = context.Request.Path.Value,
                statusCode = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            },
            occurredOn,
            messageId
        });

        await outbox.AddAsync(
            messageId,
            eventType,
            payload,
            aggregateType: "http_mutation",
            aggregateId: null,
            occurredOn,
            context.RequestAborted);
    }
}
