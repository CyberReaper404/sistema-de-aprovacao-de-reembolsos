using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Payments;

public sealed record PaymentListQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CostCenterId = null,
    PaymentMethod? PaymentMethod = null,
    DateTimeOffset? PaidFrom = null,
    DateTimeOffset? PaidTo = null,
    string? RequestNumber = null,
    string Sort = "paidAt:desc");
