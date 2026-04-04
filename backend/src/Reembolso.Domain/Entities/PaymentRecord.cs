using Reembolso.Domain.Enums;
using Reembolso.Domain.Exceptions;

namespace Reembolso.Domain.Entities;

public class PaymentRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public ReimbursementRequest? Request { get; private set; }
    public Guid PaidByUserId { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal AmountPaid { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PaymentRecord()
    {
    }

    public PaymentRecord(
        Guid requestId,
        Guid paidByUserId,
        DateTimeOffset paidAt,
        PaymentMethod paymentMethod,
        string paymentReference,
        decimal amountPaid,
        string? notes,
        DateTimeOffset now)
    {
        if (amountPaid <= 0)
        {
            throw new DomainRuleException("O valor pago deve ser maior que zero.");
        }

        RequestId = requestId;
        PaidByUserId = paidByUserId;
        PaidAt = paidAt;
        PaymentMethod = paymentMethod;
        PaymentReference = paymentReference.Trim();
        AmountPaid = amountPaid;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = now;
    }
}

