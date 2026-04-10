import { HttpClient } from "@/services/http/http-client";
import type {
  AdminAuditListResponse,
  AdminCategory,
  AdminCostCenter,
  AdminUserListResponse
} from "@/types/admin";

export class AdminService {
  constructor(private readonly httpClient: HttpClient) {}

  public getUsers(page = 1, pageSize = 8) {
    return this.httpClient.get<AdminUserListResponse>("/admin/users", { page, pageSize });
  }

  public getCategories() {
    return this.httpClient.get<AdminCategory[]>("/admin/categories");
  }

  public getCostCenters() {
    return this.httpClient.get<AdminCostCenter[]>("/admin/cost-centers");
  }

  public getAuditEntries(page = 1, pageSize = 8) {
    return this.httpClient.get<AdminAuditListResponse>("/admin/audit-entries", { page, pageSize });
  }
}
