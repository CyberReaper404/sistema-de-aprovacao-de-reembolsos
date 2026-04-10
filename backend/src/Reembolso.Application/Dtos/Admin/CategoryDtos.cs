namespace Reembolso.Application.Dtos.Admin;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount,
    bool ReceiptRequiredAlways,
    int? SubmissionDeadlineDays);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount,
    bool ReceiptRequiredAlways,
    int? SubmissionDeadlineDays,
    bool IsActive);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount,
    bool ReceiptRequiredAlways,
    int? SubmissionDeadlineDays);
