using Microsoft.EntityFrameworkCore;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Payments;
using Reembolso.Application.Exceptions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public PaymentService(AppDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<PagedResult<PaymentListItemResponse>> GetPagedAsync(PaymentListQuery query, CancellationToken cancellationToken)
    {
        EnsurePaymentReadAccess();

        IQueryable<PaymentRecord> dbQuery = _dbContext.PaymentRecords
            .Include(x => x.Request)!
                .ThenInclude(x => x!.Category)
            .Include(x => x.Request)!
                .ThenInclude(x => x!.CostCenter);

        if (query.CostCenterId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Request != null && x.Request.CostCenterId == query.CostCenterId.Value);
        }

        if (query.PaymentMethod.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.PaymentMethod == query.PaymentMethod.Value);
        }

        if (query.PaidFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.PaidAt >= query.PaidFrom.Value);
        }

        if (query.PaidTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.PaidAt <= query.PaidTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RequestNumber))
        {
            var requestNumber = query.RequestNumber.Trim();
            dbQuery = dbQuery.Where(x => x.Request != null && x.Request.RequestNumber.Contains(requestNumber));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var payments = await dbQuery
            .Select(x => new PaymentListItemResponse(
                x.Id,
                x.RequestId,
                x.Request!.RequestNumber,
                x.Request.Title,
                x.Request.CostCenterId,
                x.Request.CostCenter!.Code,
                x.Request.CategoryId,
                x.Request.Category!.Name,
                x.AmountPaid,
                x.Request.Currency,
                x.PaymentMethod,
                x.PaymentReference,
                x.PaidByUserId,
                x.PaidAt,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var items = ApplyOrdering(payments, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedResult<PaymentListItemResponse>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<PaymentDetailResponse> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        EnsurePaymentReadAccess();

        var payment = await _dbContext.PaymentRecords
            .Include(x => x.Request)!
                .ThenInclude(x => x!.Category)
            .Include(x => x.Request)!
                .ThenInclude(x => x!.CostCenter)
            .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new NotFoundAppException("Pagamento não encontrado.");

        return new PaymentDetailResponse(
            payment.Id,
            payment.RequestId,
            payment.Request!.RequestNumber,
            payment.Request.Title,
            payment.Request.CostCenterId,
            payment.Request.CostCenter!.Code,
            payment.Request.CategoryId,
            payment.Request.Category!.Name,
            payment.AmountPaid,
            payment.Request.Currency,
            payment.PaymentMethod,
            payment.PaymentReference,
            payment.Notes,
            payment.PaidByUserId,
            payment.PaidAt,
            payment.CreatedAt);
    }

    private void EnsurePaymentReadAccess()
    {
        if (_currentUserContext.Role is UserRole.Finance or UserRole.Administrator)
        {
            return;
        }

        throw new ForbiddenAppException("O usuário não possui permissão para consultar pagamentos.");
    }

    private static IEnumerable<PaymentListItemResponse> ApplyOrdering(IEnumerable<PaymentListItemResponse> query, string sort)
    {
        var parts = sort.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var field = parts.ElementAtOrDefault(0)?.ToLowerInvariant() ?? "paidat";
        var direction = parts.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "desc";
        var desc = direction != "asc";

        return field switch
        {
            "amount" => desc ? query.OrderByDescending(x => x.AmountPaid) : query.OrderBy(x => x.AmountPaid),
            "createdat" => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => desc ? query.OrderByDescending(x => x.PaidAt) : query.OrderBy(x => x.PaidAt)
        };
    }
}
