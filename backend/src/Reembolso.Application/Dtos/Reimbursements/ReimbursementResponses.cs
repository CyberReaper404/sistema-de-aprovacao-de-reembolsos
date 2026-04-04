using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record ReimbursementListItemResponse(
    Guid Id,
    string RequestNumber,
    string Title,
    string CategoryName,
    decimal Amount,
    string Currency,
    RequestStatus Status,
    DateOnly ExpenseDate,
    string CostCenterCode,
    DateTimeOffset CreatedAt);

public sealed record ReimbursementDetailResponse(
    Guid Id,
    string RequestNumber,
    string Title,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string Description,
    Guid CostCenterId,
    string CostCenterCode,
    RequestStatus Status,
    Guid CreatedByUserId,
    string CreatedByUserName,
    Guid? ApprovedByUserId,
    Guid? PaidByUserId,
    string? RejectionReason,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? RejectedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion,
    ReimbursementAllowedActions AllowedActions,
    IReadOnlyCollection<AttachmentResponse> Attachments,
    IReadOnlyCollection<WorkflowActionResponse> WorkflowActions);

public sealed record ReimbursementAllowedActions(
    bool CanEditDraft,
    bool CanSubmit,
    bool CanApprove,
    bool CanReject,
    bool CanRecordPayment,
    bool CanUploadAttachment,
    bool CanDeleteAttachment);

public sealed record AttachmentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset CreatedAt);

public sealed record WorkflowActionResponse(
    Guid Id,
    WorkflowActionType ActionType,
    RequestStatus? FromStatus,
    RequestStatus ToStatus,
    Guid PerformedByUserId,
    string? Comment,
    DateTimeOffset OccurredAt);

