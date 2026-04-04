using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record RecordPaymentRequest(
    PaymentMethod PaymentMethod,
    string PaymentReference,
    DateTimeOffset PaidAt,
    decimal AmountPaid,
    string? Notes);

