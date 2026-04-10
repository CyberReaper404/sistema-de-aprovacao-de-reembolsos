namespace Reembolso.Domain.Enums;

public enum WorkflowActionType
{
    DraftCreated = 1,
    DraftUpdated = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    AttachmentAdded = 6,
    AttachmentRemoved = 7,
    PaymentRegistered = 8,
    ComplementationRequested = 9
}
