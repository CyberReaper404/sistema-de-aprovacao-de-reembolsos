using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Payments;

public sealed record PaymentListItemResponse(
    Guid Id,
    Guid RequestId,
    string RequestNumber,
    string RequestTitle,
    Guid CostCenterId,
    string CostCenterCode,
    Guid CategoryId,
    string CategoryName,
    decimal AmountPaid,
    string Currency,
    PaymentMethod PaymentMethod,
    string PaymentReference,
    Guid PaidByUserId,
    DateTimeOffset PaidAt,
    DateTimeOffset CreatedAt);

public sealed record PaymentDetailResponse(
    Guid Id,
    Guid RequestId,
    string RequestNumber,
    string RequestTitle,
    Guid CostCenterId,
    string CostCenterCode,
    Guid CategoryId,
    string CategoryName,
    decimal AmountPaid,
    string Currency,
    PaymentMethod PaymentMethod,
    string PaymentReference,
    string? Notes,
    Guid PaidByUserId,
    DateTimeOffset PaidAt,
    DateTimeOffset CreatedAt);
