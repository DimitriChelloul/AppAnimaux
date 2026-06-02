using Microsoft.Extensions.Options;

namespace ApiGatewayService.Api.Gateway;

public sealed class GatewayProxy
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host"
    };

    private readonly HttpClient _http;
    private readonly JwtValidator _jwtValidator;
    private readonly GatewayRoutesOptions _routes;

    public GatewayProxy(HttpClient http, JwtValidator jwtValidator, IOptions<GatewayRoutesOptions> routes)
    {
        _http = http;
        _jwtValidator = jwtValidator;
        _routes = routes.Value;
    }

    public async Task ProxyAsync(HttpContext context, CancellationToken ct)
    {
        var route = ResolveRoute(context.Request.Path);
        if (route is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "No gateway route matches this path." }, ct);
            return;
        }

        AuthenticatedUser? user = null;
        if (!route.IsPublic)
        {
            if (!_jwtValidator.TryValidate(context.Request.Headers.Authorization.ToString(), out var authenticatedUser))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid access token." }, ct);
                return;
            }

            user = authenticatedUser;
        }

        using var request = CreateDownstreamRequest(context, route, user);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await CopyResponseAsync(context, response, ct);
    }

    private GatewayRoute? ResolveRoute(PathString path)
    {
        var value = path.Value ?? "/";

        if (value.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.IdentityService), IsPublic: true);
        }

        if (value.StartsWith("/profiles", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/user-profiles", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.UserProfileService), IsPublic: false);
        }

        if (value.StartsWith("/pets", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.PetService), IsPublic: false);
        }

        if (value.StartsWith("/media", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/frontend-assets", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.MediaService), IsPublic: false);
        }

        if (value.StartsWith("/professionals", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.ProfessionalService), IsPublic: false);
        }

        if (value.StartsWith("/reviews", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.ReviewService), IsPublic: false);
        }

        if (value.StartsWith("/forum", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.ForumService), IsPublic: false);
        }

        if (value.StartsWith("/help-requests", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.HelpRequestService), IsPublic: false);
        }

        if (value.StartsWith("/notifications", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.AlertService), IsPublic: false);
        }

        if (value.StartsWith("/conversations", StringComparison.OrdinalIgnoreCase))
        {
            return new GatewayRoute(new Uri(_routes.PrivateMessagingService), IsPublic: false);
        }

        return null;
    }

    private static HttpRequestMessage CreateDownstreamRequest(HttpContext context, GatewayRoute route, AuthenticatedUser? user)
    {
        var targetUri = new UriBuilder(route.BaseUri)
        {
            Path = context.Request.Path,
            Query = context.Request.QueryString.Value
        }.Uri;

        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            request.Content = new StreamContent(context.Request.Body);
        }

        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (user is not null)
        {
            request.Headers.Remove("X-User-Id");
            request.Headers.Remove("X-User-Email");
            request.Headers.Remove("X-User-Roles");
            request.Headers.TryAddWithoutValidation("X-User-Id", user.UserId.ToString());
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                request.Headers.TryAddWithoutValidation("X-User-Email", user.Email);
            }

            request.Headers.TryAddWithoutValidation("X-User-Roles", string.Join(",", user.Roles));
        }

        return request;
    }

    private static async Task CopyResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken ct)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in response.Content.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, ct);
    }

    private sealed record GatewayRoute(Uri BaseUri, bool IsPublic);
}
