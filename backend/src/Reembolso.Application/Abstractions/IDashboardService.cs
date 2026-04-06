using Reembolso.Application.Dtos.Dashboard;

namespace Reembolso.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DashboardByCategoryItemResponse>> GetByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DashboardByStatusItemResponse>> GetByStatusAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DashboardByPeriodItemResponse>> GetByPeriodAsync(DashboardByPeriodQuery query, CancellationToken cancellationToken);
}
