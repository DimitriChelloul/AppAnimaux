using Shared.Messaging.Extensions;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseTransactionalOutbox();

app.Run();
