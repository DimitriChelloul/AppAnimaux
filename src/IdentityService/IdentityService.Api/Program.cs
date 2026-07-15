using Shared.Security;
using IdentityService.BLL.Options;
using IdentityService.BLL.Security;
using IdentityService.BLL.Services;
using IdentityService.DAL.Repositories;
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
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddOutboxMessaging(builder.Configuration);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
