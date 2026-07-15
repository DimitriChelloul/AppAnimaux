using LocationService.BLL.Services;
using LocationService.DAL.Repositories;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;
using Shared.Messaging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationAppService, LocationAppService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.UseGenericMutationOutbox("LocationService");
app.MapControllers();

app.Run();
