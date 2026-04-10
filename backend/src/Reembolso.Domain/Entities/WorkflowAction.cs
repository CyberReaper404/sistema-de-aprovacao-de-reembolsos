using Reembolso.Domain.Enums;

namespace Reembolso.Domain.Entities;

public class WorkflowAction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public ReimbursementRequest? Request { get; private set; }
    public WorkflowActionType ActionType { get; private set; }
    public RequestStatus? FromStatus { get; private set; }
    public RequestStatus ToStatus { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DecisionReasonCode? ReasonCode { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;

    private WorkflowAction()
    {
    }

    public WorkflowAction(
        Guid requestId,
        WorkflowActionType actionType,
        RequestStatus? fromStatus,
        RequestStatus toStatus,
        Guid performedByUserId,
        DecisionReasonCode? reasonCode,
        string? comment,
        DateTimeOffset occurredAt)
    {
        RequestId = requestId;
        ActionType = actionType;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        PerformedByUserId = performedByUserId;
        ReasonCode = reasonCode;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        OccurredAt = occurredAt;
    }
}
