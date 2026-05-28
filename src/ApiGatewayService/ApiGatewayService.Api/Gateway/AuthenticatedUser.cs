namespace ApiGatewayService.Api.Gateway;

public sealed record AuthenticatedUser(Guid UserId, string? Email, IReadOnlyCollection<string> Roles);
