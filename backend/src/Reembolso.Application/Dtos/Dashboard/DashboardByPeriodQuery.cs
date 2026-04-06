namespace Reembolso.Application.Dtos.Dashboard;

public sealed record DashboardByPeriodQuery(
    DateOnly? From,
    DateOnly? To,
    DashboardPeriodGrouping GroupBy = DashboardPeriodGrouping.Month);

public enum DashboardPeriodGrouping
{
    Day = 1,
    Month = 2
}
