using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Auth;

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User,
    string RefreshToken);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    Guid PrimaryCostCenterId);

