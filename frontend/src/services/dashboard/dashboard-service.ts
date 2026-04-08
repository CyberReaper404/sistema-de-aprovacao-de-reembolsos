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

  public getSummary() {
    return this.httpClient.get<DashboardSummary>("/dashboard/summary");
  }

  public getByCategory() {
    return this.httpClient.get<DashboardByCategoryItem[]>("/dashboard/by-category");
  }

  public getByStatus() {
    return this.httpClient.get<DashboardByStatusItem[]>("/dashboard/by-status");
  }

  public getByPeriod(query: DashboardByPeriodQuery) {
    return this.httpClient.get<DashboardByPeriodItem[]>("/dashboard/by-period", query);
  }
}
