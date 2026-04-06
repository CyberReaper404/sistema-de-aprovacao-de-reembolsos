using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Dashboard;

public sealed record DashboardSummaryResponse(
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int PaidRequests,
    decimal TotalApprovedAmount,
    decimal TotalPaidAmount);

public sealed record DashboardByCategoryItemResponse(
    Guid CategoryId,
    string CategoryName,
    int TotalRequests,
    decimal TotalAmount);

public sealed record DashboardByStatusItemResponse(
    RequestStatus Status,
    int TotalRequests,
    decimal TotalAmount);

public sealed record DashboardByPeriodItemResponse(
    DateOnly PeriodStart,
    DashboardPeriodGrouping GroupBy,
    int TotalRequests,
    decimal TotalAmount,
    int PaidRequests,
    decimal PaidAmount);
