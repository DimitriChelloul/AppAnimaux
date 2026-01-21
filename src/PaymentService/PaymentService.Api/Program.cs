using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection; // Ajouté pour les méthodes d'extension Swagger
using PaymentService.BLL.Services;
using PaymentService.DAL.Repositories;
using Shared.Contracts.Messaging;
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

// DAL
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// BLL
builder.Services.AddScoped<IPaymentAppService, PaymentAppService>();

// RabbitMQ (publisher + outbox publisher)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<Shared.Messaging.RabbitMq.IRabbitMqConnection, Shared.Messaging.RabbitMq.RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, Shared.Messaging.RabbitMq.RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventRoutingMapper, DefaultEventRoutingMapper>();

// Publie outbox_messages -> RabbitMQ
builder.Services.AddHostedService<OutboxPublisherHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
