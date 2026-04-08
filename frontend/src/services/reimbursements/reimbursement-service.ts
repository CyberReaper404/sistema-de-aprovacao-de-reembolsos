import { HttpClient } from "@/services/http/http-client";
import type {
  Attachment,
  ReimbursementDetail,
  ReimbursementListQuery,
  ReimbursementListResponse,
  WorkflowAction
} from "@/types/reimbursements";

export class ReimbursementService {
  constructor(private readonly httpClient: HttpClient) {}

  public getPaged(query: ReimbursementListQuery) {
    return this.httpClient.get<ReimbursementListResponse>("/reimbursements", query);
  }

  public getById(id: string) {
    return this.httpClient.get<ReimbursementDetail>(`/reimbursements/${id}`);
  }

  public getAttachments(id: string) {
    return this.httpClient.get<Attachment[]>(`/reimbursements/${id}/attachments`);
  }

  public getWorkflowActions(id: string) {
    return this.httpClient.get<WorkflowAction[]>(`/reimbursements/${id}/workflow-actions`);
  }
}
