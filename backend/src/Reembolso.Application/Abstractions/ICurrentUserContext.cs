using Reembolso.Domain.Enums;

namespace Reembolso.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    int? SessionVersion { get; }
    bool IsAuthenticated { get; }
}

