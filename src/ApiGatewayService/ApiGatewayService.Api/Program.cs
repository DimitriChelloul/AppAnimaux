using ApiGatewayService.Api.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GatewayRoutesOptions>(builder.Configuration.GetSection("GatewayRoutes"));
builder.Services.AddSingleton<JwtValidator>();
builder.Services.AddHttpClient<GatewayProxy>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ApiGatewayService" }));

app.Map("{**path}", async (HttpContext context, GatewayProxy proxy, CancellationToken ct) =>
{
    await proxy.ProxyAsync(context, ct);
});

app.Run();
