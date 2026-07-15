using AdminService.BLL.Services;
using AdminService.DAL.Repositories;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;
using Shared.Messaging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminAppService, AdminAppService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
