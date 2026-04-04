namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record CreateReimbursementRequest(
    string Title,
    Guid CategoryId,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string Description);

