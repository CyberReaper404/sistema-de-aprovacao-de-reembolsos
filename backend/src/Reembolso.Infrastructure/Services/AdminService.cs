using Microsoft.EntityFrameworkCore;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Admin;
using Reembolso.Application.Exceptions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Services;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public AdminService(
        AppDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var size = Math.Clamp(pageSize, 1, 100);
        var currentPage = Math.Max(1, page);
        var query = _dbContext.Users.Include(x => x.ManagedCostCenters).AsNoTracking();
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.FullName)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(x => new UserResponse(x.Id, x.FullName, x.Email, x.Role, x.PrimaryCostCenterId, x.IsActive, x.ManagedCostCenters.Select(scope => scope.CostCenterId).ToArray()))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserResponse>(items, currentPage, size, totalItems, (int)Math.Ceiling(totalItems / (double)size));
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictAppException("Já existe usuário com este e-mail.");
        }

        await EnsureCostCenterExistsAsync(request.PrimaryCostCenterId, cancellationToken);
        await EnsureManagedCostCentersAreActiveAsync(request.ManagedCostCenterIds, cancellationToken);

        var user = new User(
            request.FullName,
            normalizedEmail,
            _passwordHasherService.HashPassword(request.Password),
            request.Role,
            request.PrimaryCostCenterId,
            _dateTimeProvider.UtcNow);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await ReplaceManagerScopesAsync(user.Id, request.ManagedCostCenterIds, cancellationToken);
        await _auditService.WriteAsync("admin.user_created", "user", user.Id.ToString(), AuditSeverity.Information, null, cancellationToken);

        return await GetUserResponseAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundAppException("Usuário não encontrado.");

        await EnsureCostCenterExistsAsync(request.PrimaryCostCenterId, cancellationToken);
        await EnsureManagedCostCentersAreActiveAsync(request.ManagedCostCenterIds, cancellationToken);

        user.Update(request.FullName, request.Role, request.PrimaryCostCenterId, request.IsActive, _dateTimeProvider.UtcNow);
        await ReplaceManagerScopesAsync(user.Id, request.ManagedCostCenterIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.user_updated", "user", user.Id.ToString(), AuditSeverity.Information, null, cancellationToken);

        return await GetUserResponseAsync(user.Id, cancellationToken);
    }

    public async Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundAppException("Usuário não encontrado.");

        user.RevokeAllSessions(_dateTimeProvider.UtcNow);
        var sessions = await _dbContext.RefreshSessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(_dateTimeProvider.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.user_sessions_revoked", "user", user.Id.ToString(), AuditSeverity.Warning, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CostCenterResponse>> GetCostCentersAsync(CancellationToken cancellationToken)
    {
        EnsureAdmin();
        return await _dbContext.CostCenters
            .OrderBy(x => x.Code)
            .Select(x => new CostCenterResponse(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CostCenterResponse> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var entity = new CostCenter(request.Code, request.Name, _dateTimeProvider.UtcNow);
        _dbContext.CostCenters.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.cost_center_created", "cost_center", entity.Id.ToString(), AuditSeverity.Information, new
        {
            entity.Code,
            entity.Name
        }, cancellationToken);

        return new CostCenterResponse(entity.Id, entity.Code, entity.Name, entity.IsActive);
    }

    public async Task<CostCenterResponse> UpdateCostCenterAsync(Guid costCenterId, UpdateCostCenterRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var entity = await _dbContext.CostCenters.SingleOrDefaultAsync(x => x.Id == costCenterId, cancellationToken)
            ?? throw new NotFoundAppException("Centro de custo não encontrado.");

        entity.Update(request.Code, request.Name, request.IsActive, _dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.cost_center_updated", "cost_center", entity.Id.ToString(), AuditSeverity.Information, new
        {
            entity.Code,
            entity.Name,
            entity.IsActive
        }, cancellationToken);

        return new CostCenterResponse(entity.Id, entity.Code, entity.Name, entity.IsActive);
    }

    public async Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        EnsureAdmin();
        return await _dbContext.ReimbursementCategories
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse(
                x.Id,
                x.Name,
                x.Description,
                x.IsActive,
                x.MaxAmount,
                x.ReceiptRequiredAboveAmount,
                x.ReceiptRequiredAlways,
                x.SubmissionDeadlineDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var entity = new ReimbursementCategory(
            request.Name,
            request.Description,
            request.MaxAmount,
            request.ReceiptRequiredAboveAmount,
            request.ReceiptRequiredAlways,
            request.SubmissionDeadlineDays,
            _dateTimeProvider.UtcNow);
        _dbContext.ReimbursementCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.category_created", "reimbursement_category", entity.Id.ToString(), AuditSeverity.Information, new
        {
            entity.Name,
            entity.MaxAmount,
            entity.ReceiptRequiredAboveAmount,
            entity.ReceiptRequiredAlways,
            entity.SubmissionDeadlineDays
        }, cancellationToken);

        return new CategoryResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.MaxAmount,
            entity.ReceiptRequiredAboveAmount,
            entity.ReceiptRequiredAlways,
            entity.SubmissionDeadlineDays);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var entity = await _dbContext.ReimbursementCategories.SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken)
            ?? throw new NotFoundAppException("Categoria não encontrada.");

        entity.Update(
            request.Name,
            request.Description,
            request.MaxAmount,
            request.ReceiptRequiredAboveAmount,
            request.ReceiptRequiredAlways,
            request.SubmissionDeadlineDays,
            request.IsActive,
            _dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("admin.category_updated", "reimbursement_category", entity.Id.ToString(), AuditSeverity.Information, new
        {
            entity.Name,
            entity.IsActive,
            entity.MaxAmount,
            entity.ReceiptRequiredAboveAmount,
            entity.ReceiptRequiredAlways,
            entity.SubmissionDeadlineDays
        }, cancellationToken);

        return new CategoryResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.MaxAmount,
            entity.ReceiptRequiredAboveAmount,
            entity.ReceiptRequiredAlways,
            entity.SubmissionDeadlineDays);
    }

    public async Task<PagedResult<AuditEntryResponse>> GetAuditEntriesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var size = Math.Clamp(pageSize, 1, 100);
        var currentPage = Math.Max(1, page);
        var query = _dbContext.AuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAt);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(x => new AuditEntryResponse(x.Id, x.EventType, x.EntityType, x.EntityId, x.ActorUserId, x.Severity, x.OccurredAt, x.MetadataJson))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryResponse>(items, currentPage, size, totalItems, (int)Math.Ceiling(totalItems / (double)size));
    }

    private void EnsureAdmin()
    {
        if (_currentUserContext.Role != UserRole.Administrator)
        {
            throw new ForbiddenAppException("Acesso permitido somente para administradores.");
        }
    }

    private async Task EnsureCostCenterExistsAsync(Guid costCenterId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.CostCenters.AnyAsync(x => x.Id == costCenterId && x.IsActive, cancellationToken))
        {
            throw new ValidationAppException("Centro de custo inválido.", new Dictionary<string, string[]>
            {
                ["primaryCostCenterId"] = ["Centro de custo inválido."]
            });
        }
    }

    private async Task EnsureManagedCostCentersAreActiveAsync(IReadOnlyCollection<Guid>? managedCostCenterIds, CancellationToken cancellationToken)
    {
        if (managedCostCenterIds is null || managedCostCenterIds.Count == 0)
        {
            return;
        }

        var distinctIds = managedCostCenterIds.Distinct().ToArray();
        var activeIds = await _dbContext.CostCenters
            .Where(x => x.IsActive && distinctIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (activeIds.Count != distinctIds.Length)
        {
            throw new ValidationAppException("Os centros de custo informados para o gestor são inválidos.", new Dictionary<string, string[]>
            {
                ["managedCostCenterIds"] = ["Informe apenas centros de custo existentes e ativos."]
            });
        }
    }

    private async Task ReplaceManagerScopesAsync(Guid userId, IReadOnlyCollection<Guid>? managedCostCenterIds, CancellationToken cancellationToken)
    {
        var currentScopes = await _dbContext.ManagerCostCenterScopes.Where(x => x.ManagerId == userId).ToListAsync(cancellationToken);
        _dbContext.ManagerCostCenterScopes.RemoveRange(currentScopes);

        if (managedCostCenterIds is not null)
        {
            foreach (var costCenterId in managedCostCenterIds.Distinct())
            {
                _dbContext.ManagerCostCenterScopes.Add(new ManagerCostCenterScope(userId, costCenterId, _dateTimeProvider.UtcNow));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserResponse> GetUserResponseAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.Include(x => x.ManagedCostCenters)
            .Where(x => x.Id == userId)
            .Select(x => new UserResponse(x.Id, x.FullName, x.Email, x.Role, x.PrimaryCostCenterId, x.IsActive, x.ManagedCostCenters.Select(scope => scope.CostCenterId).ToArray()))
            .SingleAsync(cancellationToken);
    }
}
