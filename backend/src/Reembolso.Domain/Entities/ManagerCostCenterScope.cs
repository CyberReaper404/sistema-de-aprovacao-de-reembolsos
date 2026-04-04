namespace Reembolso.Domain.Entities;

public class ManagerCostCenterScope
{
    public Guid ManagerId { get; private set; }
    public User? Manager { get; private set; }
    public Guid CostCenterId { get; private set; }
    public CostCenter? CostCenter { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ManagerCostCenterScope()
    {
    }

    public ManagerCostCenterScope(Guid managerId, Guid costCenterId, DateTimeOffset now)
    {
        ManagerId = managerId;
        CostCenterId = costCenterId;
        CreatedAt = now;
    }
}

