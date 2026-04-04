using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Admin;

public sealed record AuditEntryResponse(
    Guid Id,
    string EventType,
    string EntityType,
    string? EntityId,
    Guid? ActorUserId,
    AuditSeverity Severity,
    DateTimeOffset OccurredAt,
    string? MetadataJson);
