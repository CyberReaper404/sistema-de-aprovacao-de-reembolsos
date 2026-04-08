import { HttpClient } from "@/services/http/http-client";
import type {
  Attachment,
  CreateReimbursementPayload,
  ReimbursementDetail,
  ReimbursementCategoryOption,
  ReimbursementListQuery,
  ReimbursementListResponse,
  UpdateReimbursementDraftPayload,
  WorkflowAction
} from "@/types/reimbursements";

export class ReimbursementService {
  constructor(private readonly httpClient: HttpClient) {}

  public getAvailableCategories() {
    return this.httpClient.get<ReimbursementCategoryOption[]>("/reimbursements/categories");
  }

  public getPaged(query: ReimbursementListQuery) {
    return this.httpClient.get<ReimbursementListResponse>("/reimbursements", query);
  }

  public create(payload: CreateReimbursementPayload) {
    return this.httpClient.post<ReimbursementDetail>("/reimbursements", payload);
  }

  public getById(id: string) {
    return this.httpClient.get<ReimbursementDetail>(`/reimbursements/${id}`);
  }

  public updateDraft(id: string, payload: UpdateReimbursementDraftPayload) {
    return this.httpClient.put<ReimbursementDetail>(`/reimbursements/${id}`, payload);
  }

  public submit(id: string) {
    return this.httpClient.post<void>(`/reimbursements/${id}/submit`);
  }

  public getAttachments(id: string) {
    return this.httpClient.get<Attachment[]>(`/reimbursements/${id}/attachments`);
  }

  public addAttachment(id: string, file: File) {
    const formData = new FormData();
    formData.append("file", file);
    return this.httpClient.post<Attachment>(`/reimbursements/${id}/attachments`, formData);
  }

  public deleteAttachment(id: string, attachmentId: string) {
    return this.httpClient.delete<void>(`/reimbursements/${id}/attachments/${attachmentId}`);
  }

  public getWorkflowActions(id: string) {
    return this.httpClient.get<WorkflowAction[]>(`/reimbursements/${id}/workflow-actions`);
  }

  public downloadAttachment(id: string, attachmentId: string) {
    return this.httpClient.download(`/reimbursements/${id}/attachments/${attachmentId}`);
  }
}
