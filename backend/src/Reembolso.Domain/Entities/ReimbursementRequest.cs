using Reembolso.Domain.Enums;
using Reembolso.Domain.Exceptions;

namespace Reembolso.Domain.Entities;

public class ReimbursementRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string RequestNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public ReimbursementCategory? Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public DateOnly ExpenseDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid CostCenterId { get; private set; }
    public CostCenter? CostCenter { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Draft;
    public Guid CreatedByUserId { get; private set; }
    public User? CreatedByUser { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public Guid? PaidByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public Guid RowVersion { get; private set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<ReimbursementAttachment> Attachments { get; private set; } = new List<ReimbursementAttachment>();
    public ICollection<WorkflowAction> WorkflowActions { get; private set; } = new List<WorkflowAction>();
    public PaymentRecord? PaymentRecord { get; private set; }

    private ReimbursementRequest()
    {
    }

    public ReimbursementRequest(
        string requestNumber,
        string title,
        Guid categoryId,
        decimal amount,
        string currency,
        DateOnly expenseDate,
        string description,
        Guid costCenterId,
        Guid createdByUserId,
        DateTimeOffset now)
    {
        RequestNumber = requestNumber;
        Title = title.Trim();
        CategoryId = categoryId;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        ExpenseDate = expenseDate;
        Description = description.Trim();
        CostCenterId = costCenterId;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
        UpdatedAt = now;
        RowVersion = Guid.NewGuid();
    }

    public void UpdateDraft(
        string title,
        Guid categoryId,
        decimal amount,
        string currency,
        DateOnly expenseDate,
        string description,
        DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Draft, "Somente rascunhos podem ser editados.");

        Title = title.Trim();
        CategoryId = categoryId;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        ExpenseDate = expenseDate;
        Description = description.Trim();
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void Submit(DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Draft, "Somente rascunhos podem ser enviados.");

        Status = RequestStatus.Submitted;
        SubmittedAt = now;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "A solicitação precisa estar enviada para ser aprovada.");

        Status = RequestStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = now;
        RejectionReason = null;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void Reject(Guid approvedByUserId, string reason, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "A solicitação precisa estar enviada para ser recusada.");

        Status = RequestStatus.Rejected;
        ApprovedByUserId = approvedByUserId;
        RejectedAt = now;
        RejectionReason = reason.Trim();
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void RegisterPayment(Guid paidByUserId, DateTimeOffset paidAt, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Approved, "Somente solicitações aprovadas podem ser pagas.");

        Status = RequestStatus.Paid;
        PaidByUserId = paidByUserId;
        PaidAt = paidAt;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    private void EnsureStatus(RequestStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new DomainRuleException(message);
        }
    }

    private void TouchConcurrencyToken()
    {
        RowVersion = Guid.NewGuid();
    }
}
