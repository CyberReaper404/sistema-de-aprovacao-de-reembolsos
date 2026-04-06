using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Auth;
using Reembolso.Application.Exceptions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Options;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService,
        ICurrentUserContext currentUserContext,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
        _currentUserContext = currentUserContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthSessionResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasherService.VerifyHashedPassword(user.PasswordHash, request.Password))
        {
            await _auditService.WriteAsync(
                "auth.login_failed",
                "user",
                user?.Id.ToString(),
                AuditSeverity.Warning,
                new { normalizedEmail },
                cancellationToken);

            throw new UnauthorizedAppException("Credenciais inválidas.", "invalid_credentials");
        }

        var now = _dateTimeProvider.UtcNow;
        var refreshToken = _tokenService.GenerateRefreshToken();
        var session = new RefreshSession(
            user.Id,
            _tokenService.HashRefreshToken(refreshToken),
            Guid.NewGuid(),
            now.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            ipAddress,
            userAgent,
            now);

        _dbContext.RefreshSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "auth.login_succeeded",
            "user",
            user.Id.ToString(),
            AuditSeverity.Information,
            null,
            cancellationToken);

        return BuildSessionResponse(user, refreshToken, now);
    }

    public async Task<AuthSessionResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var hash = _tokenService.HashRefreshToken(refreshToken);

        var session = await _dbContext.RefreshSessions
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (session is null || session.User is null)
        {
            await _auditService.WriteAsync(
                "auth.refresh_failed",
                "refresh_session",
                null,
                AuditSeverity.Warning,
                new { reason = "not_found" },
                cancellationToken);

            throw new UnauthorizedAppException("Sessão inválida.", "invalid_refresh_token");
        }

        if (session.IsRevoked())
        {
            await RevokeFamilyAsync(session.FamilyId, cancellationToken);
            await _auditService.WriteAsync(
                "auth.refresh_reuse_detected",
                "refresh_session",
                session.Id.ToString(),
                AuditSeverity.Critical,
                null,
                cancellationToken);

            throw new UnauthorizedAppException("Sessão revogada.", "revoked_refresh_token");
        }

        if (session.IsExpired(now) || !session.User.IsActive)
        {
            session.Revoke(now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.WriteAsync(
                "auth.refresh_expired",
                "refresh_session",
                session.Id.ToString(),
                AuditSeverity.Warning,
                new { session.User.IsActive },
                cancellationToken);
            throw new UnauthorizedAppException("Sessão expirada.", "expired_refresh_token");
        }

        var rotatedRefreshToken = _tokenService.GenerateRefreshToken();
        var replacement = new RefreshSession(
            session.UserId,
            _tokenService.HashRefreshToken(rotatedRefreshToken),
            session.FamilyId,
            now.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            ipAddress,
            userAgent,
            now);

        _dbContext.RefreshSessions.Add(replacement);
        session.Rotate(replacement.Id, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildSessionResponse(session.User, rotatedRefreshToken, now);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var session = await _dbContext.RefreshSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.Revoke(_dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("auth.logout", "refresh_session", session.Id.ToString(), AuditSeverity.Information, null, cancellationToken);
    }

    public async Task LogoutAllAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var now = _dateTimeProvider.UtcNow;

        var sessions = await _dbContext.RefreshSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("auth.logout_all", "user", userId.ToString(), AuditSeverity.Information, null, cancellationToken);
    }

    public async Task<AuthenticatedUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("Usuário não encontrado.");

        return new AuthenticatedUserResponse(user.Id, user.FullName, user.Email, user.Role, user.PrimaryCostCenterId);
    }

    private Guid RequireCurrentUserId()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAppException("Usuário não autenticado.");
    }

    private AuthSessionResponse BuildSessionResponse(User user, string refreshToken, DateTimeOffset now)
    {
        var accessTokenExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays);

        return new AuthSessionResponse(
            _tokenService.CreateAccessToken(user),
            accessTokenExpiresAt,
            new AuthenticatedUserResponse(user.Id, user.FullName, user.Email, user.Role, user.PrimaryCostCenterId),
            refreshToken,
            refreshTokenExpiresAt);
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var familySessions = await _dbContext.RefreshSessions
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var familySession in familySessions)
        {
            familySession.Revoke(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
