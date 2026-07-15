using AdvertisingService.BLL.Services;
using AdvertisingService.DAL.Repositories;
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
builder.Services.AddSwaggerGen();

builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);

builder.Services.AddScoped<IAdvertisingRepository, AdvertisingRepository>();
builder.Services.AddScoped<IAdvertisingAppService, AdvertisingAppService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
