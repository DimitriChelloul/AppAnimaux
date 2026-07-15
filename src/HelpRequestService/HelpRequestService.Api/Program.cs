using HelpRequestService.BLL.Services;
using HelpRequestService.DAL.Repositories;
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
builder.Services.AddScoped<IHelpRequestRepository, HelpRequestRepository>();
builder.Services.AddScoped<IHelpRequestAppService, HelpRequestAppService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
