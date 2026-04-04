namespace Reembolso.Domain.Entities;

public class RefreshSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RotatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshSession()
    {
    }

    public RefreshSession(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        string? createdByIp,
        string? userAgent,
        DateTimeOffset now)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAt = expiresAt;
        CreatedAt = now;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public bool IsRevoked() => RevokedAt.HasValue;

    public void Rotate(Guid replacementSessionId, DateTimeOffset now)
    {
        RotatedAt = now;
        RevokedAt = now;
        ReplacedBySessionId = replacementSessionId;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
