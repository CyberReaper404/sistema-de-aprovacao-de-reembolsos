namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record UpdateReimbursementDraftRequest(
    string Title,
    Guid CategoryId,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string Description,
    string RowVersion);

