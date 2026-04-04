namespace Reembolso.Application.Dtos.Admin;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount,
    bool IsActive);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    decimal? MaxAmount,
    decimal? ReceiptRequiredAboveAmount);

