namespace Reembolso.Domain.Entities;

public class ReimbursementCategory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal? MaxAmount { get; private set; }
    public decimal? ReceiptRequiredAboveAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ReimbursementCategory()
    {
    }

    public ReimbursementCategory(
        string name,
        string? description,
        decimal? maxAmount,
        decimal? receiptRequiredAboveAmount,
        DateTimeOffset now)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MaxAmount = maxAmount;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(
        string name,
        string? description,
        decimal? maxAmount,
        decimal? receiptRequiredAboveAmount,
        bool isActive,
        DateTimeOffset now)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MaxAmount = maxAmount;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
        IsActive = isActive;
        UpdatedAt = now;
    }
}

