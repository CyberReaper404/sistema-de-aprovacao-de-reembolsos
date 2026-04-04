using Reembolso.Domain.Enums;

namespace Reembolso.Domain.Entities;

public class AuditEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? MetadataJson { get; private set; }
    public AuditSeverity Severity { get; private set; } = AuditSeverity.Information;
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;

    private AuditEntry()
    {
    }

    public AuditEntry(
        string eventType,
        string entityType,
        string? entityId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        string? metadataJson,
        AuditSeverity severity,
        DateTimeOffset occurredAt)
    {
        EventType = eventType;
        EntityType = entityType;
        EntityId = entityId;
        ActorUserId = actorUserId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        MetadataJson = metadataJson;
        Severity = severity;
        OccurredAt = occurredAt;
    }
}

