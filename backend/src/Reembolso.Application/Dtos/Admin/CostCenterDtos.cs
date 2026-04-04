namespace Reembolso.Application.Dtos.Admin;

public sealed record CreateCostCenterRequest(string Code, string Name);

public sealed record UpdateCostCenterRequest(string Code, string Name, bool IsActive);

public sealed record CostCenterResponse(Guid Id, string Code, string Name, bool IsActive);

