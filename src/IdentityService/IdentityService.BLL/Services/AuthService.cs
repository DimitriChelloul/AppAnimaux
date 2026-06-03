using System.Text.Json;
using IdentityService.BLL.Models;
using IdentityService.BLL.Options;
using IdentityService.BLL.Security;
using IdentityService.DAL.Repositories;
using Microsoft.Extensions.Options;
using Shared.Contracts.Events.Abstractions;
using Shared.Contracts.Events.Users;
using Shared.Contracts.Messaging;
using Shared.Messaging.Serialization;

namespace IdentityService.BLL.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRegistrationRepository _registrations;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _options;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IRegistrationRepository registrations,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> options)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _registrations = registrations;
        _passwordHasher = passwordHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        ValidatePassword(password);

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var userId = Guid.NewGuid();
        var passwordHash = _passwordHasher.Hash(password);
        var registered = CreateUserRegisteredEnvelope(userId, email);
        var user = await _registrations.RegisterAsync(
            userId,
            email,
            passwordHash,
            registered.MessageId,
            EventTypes.Users.UserRegistered,
            registered.Payload,
            ipAddress,
            userAgent,
            ct);

        return await IssueTokensAsync(user.Id, user.Email, ipAddress, userAgent, ct);
    }

    public async Task<AuthResult?> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null)
        {
            await _users.AddLoginAuditAsync(null, email, false, "user_not_found", ipAddress, userAgent, ct);
            return null;
        }

        if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            await _users.AddLoginAuditAsync(user.Id, email, false, "inactive_user", ipAddress, userAgent, ct);
            return null;
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            await _users.AddLoginAuditAsync(user.Id, email, false, "bad_password", ipAddress, userAgent, ct);
            return null;
        }

        await _users.MarkLoginSucceededAsync(user.Id, ct);
        await _users.AddLoginAuditAsync(user.Id, email, true, "success", ipAddress, userAgent, ct);

        return await IssueTokensAsync(user.Id, user.Email, ipAddress, userAgent, ct);
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);
        if (storedToken is null)
        {
            return null;
        }

        var user = await _users.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var result = await IssueTokensAsync(user.Id, user.Email, ipAddress, userAgent, ct);
        var replacementHash = _refreshTokenGenerator.Hash(result.RefreshToken);
        var replacement = await _refreshTokens.GetActiveByHashAsync(replacementHash, ct);
        await _refreshTokens.RevokeAsync(storedToken.Id, "rotated", ipAddress, replacement?.Id, ct);

        return result;
    }

    public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var storedToken = await _refreshTokens.GetActiveByHashAsync(tokenHash, ct);
        if (storedToken is null)
        {
            return false;
        }

        await _refreshTokens.RevokeAsync(storedToken.Id, "logout", ipAddress, null, ct);
        return true;
    }

    private async Task<AuthResult> IssueTokensAsync(Guid userId, string email, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var roles = await _users.GetRoleNamesAsync(userId, ct);
        var authUser = new AuthUser(userId, email, roles);
        var access = _jwtTokenService.CreateAccessToken(authUser);
        var refreshToken = _refreshTokenGenerator.Generate();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);

        await _refreshTokens.InsertAsync(userId, _refreshTokenGenerator.Hash(refreshToken), refreshExpiresAt, ipAddress, userAgent, ct);

        return new AuthResult(userId, email, roles, access.Token, access.ExpiresAt, refreshToken, refreshExpiresAt);
    }

    private static (Guid MessageId, string Payload) CreateUserRegisteredEnvelope(Guid userId, string email)
    {
        var evt = new UserRegisteredEvent
        {
            UserId = userId,
            Email = email,
            RegisteredAt = DateTimeOffset.UtcNow,
            SourceService = "IdentityService"
        };

        var envelope = new EventEnvelope<UserRegisteredEvent>(
            Type: EventTypes.Users.UserRegistered,
            Version: EventTypes.V1,
            Data: evt,
            OccurredOn: evt.OccurredOn,
            MessageId: evt.EventId);

        var payload = JsonSerializer.Serialize(envelope, JsonDefaults.Options);
        return (evt.EventId, payload);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must contain at least 8 characters.", nameof(password));
        }
    }
}
