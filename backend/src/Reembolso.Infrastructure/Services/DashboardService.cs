using Microsoft.EntityFrameworkCore;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Dashboard;
using Reembolso.Application.Exceptions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DashboardService(AppDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = ApplyPeriod(await BuildScopedQueryAsync(), from, to);

        var totalRequests = await query.CountAsync(cancellationToken);
        var pendingRequests = await query.CountAsync(x => x.Status == RequestStatus.Submitted, cancellationToken);
        var approvedRequests = await query.CountAsync(x => x.Status == RequestStatus.Approved, cancellationToken);
        var paidRequests = await query.CountAsync(x => x.Status == RequestStatus.Paid, cancellationToken);
        var totalApprovedAmount = await query.Where(x => x.Status == RequestStatus.Approved || x.Status == RequestStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var totalPaidAmount = await query.Where(x => x.Status == RequestStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        return new DashboardSummaryResponse(totalRequests, pendingRequests, approvedRequests, paidRequests, totalApprovedAmount, totalPaidAmount);
    }

    public async Task<IReadOnlyCollection<DashboardByCategoryItemResponse>> GetByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = ApplyPeriod(await BuildScopedQueryAsync(), from, to);
        return await query.Include(x => x.Category)
            .GroupBy(x => new { x.CategoryId, Name = x.Category!.Name })
            .Select(group => new DashboardByCategoryItemResponse(group.Key.CategoryId, group.Key.Name, group.Count(), group.Sum(x => x.Amount)))
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardByStatusItemResponse>> GetByStatusAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = ApplyPeriod(await BuildScopedQueryAsync(), from, to);
        return await query.GroupBy(x => x.Status)
            .Select(group => new DashboardByStatusItemResponse(group.Key, group.Count(), group.Sum(x => x.Amount)))
            .OrderBy(x => x.Status)
            .ToListAsync(cancellationToken);
    }

    private Task<IQueryable<ReimbursementRequest>> BuildScopedQueryAsync()
    {
        var role = _currentUserContext.Role ?? throw new ForbiddenAppException("Acesso negado ao dashboard.");
        var userId = _currentUserContext.UserId ?? throw new ForbiddenAppException("Acesso negado ao dashboard.");
        IQueryable<ReimbursementRequest> query = _dbContext.ReimbursementRequests.AsQueryable();

        return Task.FromResult(role switch
        {
            UserRole.Collaborator => query.Where(x => x.CreatedByUserId == userId),
            UserRole.Manager => query.Where(x => _dbContext.ManagerCostCenterScopes.Any(scope => scope.ManagerId == userId && scope.CostCenterId == x.CostCenterId)),
            UserRole.Finance or UserRole.Administrator => query,
            _ => throw new ForbiddenAppException("Acesso negado ao dashboard.")
        });
    }

    private static IQueryable<ReimbursementRequest> ApplyPeriod(IQueryable<ReimbursementRequest> query, DateOnly? from, DateOnly? to)
    {
        if (from.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= to.Value);
        }

        return query;
    }
}

