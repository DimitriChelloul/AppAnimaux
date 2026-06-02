using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiGatewayService.Api.Gateway;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiGatewayService.Tests;

public sealed class GatewayProxyTests
{
    private const string SigningKey = "dev-only-change-this-identity-signing-key-32-bytes-minimum";

    [Fact]
    public async Task Auth_routes_are_public_and_forwarded_to_identity()
    {
        var handler = new CapturingHandler();
        var proxy = CreateProxy(handler);
        var context = CreateContext(HttpMethods.Post, "/auth/register");

        await proxy.ProxyAsync(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://identity.test/auth/register", request.RequestUri?.ToString());
        Assert.False(request.Headers.Contains("X-User-Id"));
    }

    [Fact]
    public async Task Protected_routes_reject_missing_access_token()
    {
        var handler = new CapturingHandler();
        var proxy = CreateProxy(handler);
        var context = CreateContext(HttpMethods.Get, "/profiles/me");

        await proxy.ProxyAsync(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Profile_pet_and_location_routes_forward_authenticated_user_headers()
    {
        var userId = Guid.NewGuid();
        var token = CreateAccessToken(userId, "owner@appanimaux.test", ["Member"]);
        var handler = new CapturingHandler();
        var proxy = CreateProxy(handler);

        var profileContext = CreateContext(HttpMethods.Put, "/profiles/me", token);
        await proxy.ProxyAsync(profileContext, CancellationToken.None);

        var petContext = CreateContext(HttpMethods.Post, "/pets", token);
        await proxy.ProxyAsync(petContext, CancellationToken.None);

        var locationContext = CreateContext(HttpMethods.Put, "/locations/me", token);
        await proxy.ProxyAsync(locationContext, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, profileContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status201Created, petContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, locationContext.Response.StatusCode);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal("http://profiles.test/profiles/me", request.RequestUri?.ToString());
                Assert.Equal(userId.ToString(), Assert.Single(request.Headers.GetValues("X-User-Id")));
                Assert.Equal("owner@appanimaux.test", Assert.Single(request.Headers.GetValues("X-User-Email")));
                Assert.Equal("Member", Assert.Single(request.Headers.GetValues("X-User-Roles")));
            },
            request =>
            {
                Assert.Equal("http://pets.test/pets", request.RequestUri?.ToString());
                Assert.Equal(userId.ToString(), Assert.Single(request.Headers.GetValues("X-User-Id")));
                Assert.Equal("owner@appanimaux.test", Assert.Single(request.Headers.GetValues("X-User-Email")));
                Assert.Equal("Member", Assert.Single(request.Headers.GetValues("X-User-Roles")));
            },
            request =>
            {
                Assert.Equal("http://locations.test/locations/me", request.RequestUri?.ToString());
                Assert.Equal(userId.ToString(), Assert.Single(request.Headers.GetValues("X-User-Id")));
                Assert.Equal("owner@appanimaux.test", Assert.Single(request.Headers.GetValues("X-User-Email")));
                Assert.Equal("Member", Assert.Single(request.Headers.GetValues("X-User-Roles")));
            });
    }

    private static GatewayProxy CreateProxy(CapturingHandler handler)
    {
        var http = new HttpClient(handler);
        var jwtValidator = new JwtValidator(Options.Create(new JwtOptions
        {
            Issuer = "AppAnimaux.Identity",
            Audience = "AppAnimaux",
            SigningKey = SigningKey
        }));

        var routes = Options.Create(new GatewayRoutesOptions
        {
            IdentityService = "http://identity.test",
            UserProfileService = "http://profiles.test",
            PetService = "http://pets.test",
            LocationService = "http://locations.test"
        });

        return new GatewayProxy(http, jwtValidator, routes);
    }

    private static DefaultHttpContext CreateContext(string method, string path, string? accessToken = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (accessToken is not null)
        {
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        return context;
    }

    private static string CreateAccessToken(Guid userId, string email, string[] roles)
    {
        var now = DateTimeOffset.UtcNow;
        var header = EncodeJson(new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        });

        var payload = EncodeJson(new Dictionary<string, object>
        {
            ["iss"] = "AppAnimaux.Identity",
            ["aud"] = "AppAnimaux",
            ["sub"] = userId.ToString(),
            ["email"] = email,
            ["role"] = roles,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(15).ToUnixTimeSeconds()
        });

        var unsignedToken = $"{header}.{payload}";
        return $"{unsignedToken}.{Sign(unsignedToken)}";
    }

    private static string EncodeJson(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return EncodeBase64Url(Encoding.UTF8.GetBytes(json));
    }

    private static string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningKey));
        return EncodeBase64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string EncodeBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            var status = request.RequestUri?.AbsolutePath switch
            {
                "/auth/register" => HttpStatusCode.Created,
                "/pets" => HttpStatusCode.Created,
                _ => HttpStatusCode.OK
            };

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
