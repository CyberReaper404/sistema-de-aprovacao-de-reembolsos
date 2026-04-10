import { HttpClient } from "@/services/http/http-client";
import type {
  DashboardByCategoryItem,
  DashboardByPeriodItem,
  DashboardByPeriodQuery,
  DashboardByStatusItem,
  DashboardSummary
} from "@/types/dashboard";

export class DashboardService {
  constructor(private readonly httpClient: HttpClient) {}

  public getSummary(query?: Pick<DashboardByPeriodQuery, "from" | "to">) {
    return this.httpClient.get<DashboardSummary>("/dashboard/summary", query);
  }

  public getByCategory(query?: Pick<DashboardByPeriodQuery, "from" | "to">) {
    return this.httpClient.get<DashboardByCategoryItem[]>("/dashboard/by-category", query);
  }

  public getByStatus(query?: Pick<DashboardByPeriodQuery, "from" | "to">) {
    return this.httpClient.get<DashboardByStatusItem[]>("/dashboard/by-status", query);
  }

  public getByPeriod(query: DashboardByPeriodQuery) {
    return this.httpClient.get<DashboardByPeriodItem[]>("/dashboard/by-period", query);
  }
}
