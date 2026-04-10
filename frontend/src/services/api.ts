import { AdminService } from "@/services/admin/admin-service";
import { AuthService } from "@/services/auth/auth-service";
import { DashboardService } from "@/services/dashboard/dashboard-service";
import { HttpClient } from "@/services/http/http-client";
import { PaymentService } from "@/services/payments/payment-service";
import { ReimbursementService } from "@/services/reimbursements/reimbursement-service";

export const apiClient = new HttpClient();
export const adminService = new AdminService(apiClient);
export const authService = new AuthService(apiClient);
export const dashboardService = new DashboardService(apiClient);
export const paymentService = new PaymentService(apiClient);
export const reimbursementService = new ReimbursementService(apiClient);
