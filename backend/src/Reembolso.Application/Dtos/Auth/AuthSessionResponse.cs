using System.Text.Json.Serialization;
using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Auth;

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthenticatedUserResponse User,
    [property: JsonIgnore]
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    Guid PrimaryCostCenterId);
