using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Admin;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    Guid PrimaryCostCenterId,
    IReadOnlyCollection<Guid>? ManagedCostCenterIds);

public sealed record UpdateUserRequest(
    string FullName,
    UserRole Role,
    Guid PrimaryCostCenterId,
    bool IsActive,
    IReadOnlyCollection<Guid>? ManagedCostCenterIds);

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    Guid PrimaryCostCenterId,
    bool IsActive,
    IReadOnlyCollection<Guid> ManagedCostCenterIds);

