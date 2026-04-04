using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record ReimbursementListQuery(
    int Page = 1,
    int PageSize = 20,
    RequestStatus? Status = null,
    Guid? CategoryId = null,
    Guid? CostCenterId = null,
    DateOnly? ExpenseDateFrom = null,
    DateOnly? ExpenseDateTo = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    string? RequestNumber = null,
    bool CreatedByMe = false,
    string Sort = "createdAt:desc");

