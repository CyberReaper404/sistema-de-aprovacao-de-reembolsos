using Reembolso.Domain.Enums;

namespace Reembolso.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(
        string eventType,
        string entityType,
        string? entityId,
        AuditSeverity severity,
        object? metadata,
        CancellationToken cancellationToken);
}

