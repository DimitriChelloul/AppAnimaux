using ChatbotService.Api;
using Shared.Messaging.Extensions;
using Shared.Persistence.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddChatbotApi(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.MapHealthChecks("/api/chatbot/healthz");
app.UseTransactionalOutbox();
app.UseGenericMutationOutbox("ChatbotService");
app.MapControllers();

app.Run();
