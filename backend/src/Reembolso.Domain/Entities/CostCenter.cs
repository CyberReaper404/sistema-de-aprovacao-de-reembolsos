namespace Reembolso.Domain.Entities;

public class CostCenter
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private CostCenter()
    {
    }

    public CostCenter(string code, string name, DateTimeOffset now)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(string code, string name, bool isActive, DateTimeOffset now)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsActive = isActive;
        UpdatedAt = now;
    }
}

