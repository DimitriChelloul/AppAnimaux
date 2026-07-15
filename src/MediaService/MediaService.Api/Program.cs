using MediaService.BLL.Options;
using MediaService.BLL.Services;
using MediaService.DAL.Repositories;
using Shared.Persistence.Extensions;
using Shared.Persistence.Transactions;
using Shared.Messaging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPostgresPersistence(builder.Configuration);
builder.Services.AddOutboxMessaging(builder.Configuration);
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.PostConfigure<MediaStorageOptions>(options =>
{
    if (!Path.IsPathFullyQualified(options.RootPath))
    {
        options.RootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.RootPath));
    }
});

builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IFrontendAssetRepository, FrontendAssetRepository>();
builder.Services.AddScoped<IMediaAppService, MediaAppService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseTransactionalOutbox();
app.MapControllers();

app.Run();
