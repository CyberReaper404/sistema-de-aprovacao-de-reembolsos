using Reembolso.Application.Dtos.Auth;

namespace Reembolso.Application.Abstractions;

public interface IAuthService
{
    Task<AuthSessionResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<AuthSessionResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAllAsync(CancellationToken cancellationToken);

    Task<AuthenticatedUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
}

