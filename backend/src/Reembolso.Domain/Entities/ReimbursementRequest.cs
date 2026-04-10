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
    public DecisionReasonCode? DecisionReasonCode { get; private set; }
    public string? DecisionComment { get; private set; }
    public bool HasPendingComplementation { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ComplementationRequestedAt { get; private set; }
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
        if (Status != RequestStatus.Draft && !(Status == RequestStatus.Submitted && HasPendingComplementation))
        {
            throw new DomainRuleException("Somente rascunhos ou solicitações com complementação pendente podem ser editados.");
        }

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

    public void ResubmitAfterComplementation(DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "Somente solicitações enviadas podem reenviar complementação.");

        if (!HasPendingComplementation)
        {
            throw new DomainRuleException("A solicitação não possui complementação pendente.");
        }

        HasPendingComplementation = false;
        DecisionReasonCode = null;
        DecisionComment = null;
        ComplementationRequestedAt = null;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void RequestComplementation(Guid approvedByUserId, DecisionReasonCode reasonCode, string? comment, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "A solicitação precisa estar enviada para solicitar complementação.");

        HasPendingComplementation = true;
        ApprovedByUserId = approvedByUserId;
        DecisionReasonCode = reasonCode;
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        ComplementationRequestedAt = now;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void Approve(Guid approvedByUserId, DecisionReasonCode? reasonCode, string? comment, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "A solicitação precisa estar enviada para ser aprovada.");

        if (HasPendingComplementation)
        {
            throw new DomainRuleException("A solicitação possui complementação pendente e não pode ser aprovada.");
        }

        Status = RequestStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = now;
        RejectionReason = null;
        DecisionReasonCode = reasonCode;
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        HasPendingComplementation = false;
        ComplementationRequestedAt = null;
        UpdatedAt = now;
        TouchConcurrencyToken();
    }

    public void Reject(Guid approvedByUserId, DecisionReasonCode reasonCode, string reason, DateTimeOffset now)
    {
        EnsureStatus(RequestStatus.Submitted, "A solicitação precisa estar enviada para ser recusada.");

        Status = RequestStatus.Rejected;
        ApprovedByUserId = approvedByUserId;
        RejectedAt = now;
        RejectionReason = reason.Trim();
        DecisionReasonCode = reasonCode;
        DecisionComment = reason.Trim();
        HasPendingComplementation = false;
        ComplementationRequestedAt = null;
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
