using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Admin;

namespace Reembolso.Application.Abstractions;

public interface IAdminService
{
    Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);

    Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CostCenterResponse>> GetCostCentersAsync(CancellationToken cancellationToken);

    Task<CostCenterResponse> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken);

    Task<CostCenterResponse> UpdateCostCenterAsync(Guid costCenterId, UpdateCostCenterRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken);

    Task<PagedResult<AuditEntryResponse>> GetAuditEntriesAsync(int page, int pageSize, CancellationToken cancellationToken);
}

