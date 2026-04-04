using Reembolso.Domain.Enums;

namespace Reembolso.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid PrimaryCostCenterId { get; private set; }
    public CostCenter? PrimaryCostCenter { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SessionVersion { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<ManagerCostCenterScope> ManagedCostCenters { get; private set; } = new List<ManagerCostCenterScope>();

    private User()
    {
    }

    public User(
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        Guid primaryCostCenterId,
        DateTimeOffset now)
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        PrimaryCostCenterId = primaryCostCenterId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(
        string fullName,
        UserRole role,
        Guid primaryCostCenterId,
        bool isActive,
        DateTimeOffset now)
    {
        FullName = fullName.Trim();
        Role = role;
        PrimaryCostCenterId = primaryCostCenterId;
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void UpdatePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        SessionVersion++;
        UpdatedAt = now;
    }

    public void RevokeAllSessions(DateTimeOffset now)
    {
        SessionVersion++;
        UpdatedAt = now;
    }
}

