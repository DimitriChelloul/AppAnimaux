using AdvertisingService.BLL.Services;
using AdvertisingService.DAL.Repositories;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Outbox;
using Shared.Messaging.RabbitMq;
using Shared.Messaging.Routing;
using Shared.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPostgresPersistence(builder.Configuration);

builder.Services.AddScoped<IAdvertisingRepository, AdvertisingRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IAdvertisingAppService, AdvertisingAppService>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventRoutingMapper, DefaultEventRoutingMapper>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
