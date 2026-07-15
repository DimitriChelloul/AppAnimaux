using Microsoft.OpenApi;
using PrivateMessagingService.BLL.Services;
using PrivateMessagingService.DAL.Repositories;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Outbox;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;
using Shared.Messaging.Extensions;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "PrivateMessagingService", Version = "v1" }));
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IPrivateMessagingAppService, PrivateMessagingAppService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
