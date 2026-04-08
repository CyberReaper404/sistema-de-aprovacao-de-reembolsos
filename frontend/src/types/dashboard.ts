import type { DashboardPeriodGrouping, RequestStatus } from "@/types/domain";

export interface DashboardSummary {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  paidRequests: number;
  totalApprovedAmount: number;
  totalPaidAmount: number;
}

export interface DashboardByCategoryItem {
  categoryId: string;
  categoryName: string;
  totalRequests: number;
  totalAmount: number;
}

export interface DashboardByStatusItem {
  status: RequestStatus;
  totalRequests: number;
  totalAmount: number;
}

export interface DashboardByPeriodItem {
  periodStart: string;
  groupBy: DashboardPeriodGrouping;
  totalRequests: number;
  totalAmount: number;
  paidRequests: number;
  paidAmount: number;
}

export interface DashboardByPeriodQuery {
  from?: string;
  to?: string;
  groupBy?: DashboardPeriodGrouping;
}
