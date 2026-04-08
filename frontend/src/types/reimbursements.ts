import type { PagedResult } from "@/types/common";
import type { RequestStatus, WorkflowActionType } from "@/types/domain";

export interface ReimbursementListItem {
  id: string;
  requestNumber: string;
  title: string;
  categoryName: string;
  amount: number;
  currency: string;
  status: RequestStatus;
  expenseDate: string;
  costCenterCode: string;
  createdAt: string;
}

export interface ReimbursementCategoryOption {
  id: string;
  name: string;
  description?: string | null;
  maxAmount?: number | null;
  receiptRequiredAboveAmount?: number | null;
}

export interface ReimbursementAllowedActions {
  canEditDraft: boolean;
  canSubmit: boolean;
  canApprove: boolean;
  canReject: boolean;
  canRecordPayment: boolean;
  canUploadAttachment: boolean;
  canDeleteAttachment: boolean;
}

export interface Attachment {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeInBytes: number;
  createdAt: string;
}

export interface WorkflowAction {
  id: string;
  actionType: WorkflowActionType;
  fromStatus?: RequestStatus | null;
  toStatus: RequestStatus;
  performedByUserId: string;
  comment?: string | null;
  occurredAt: string;
}

export interface ReimbursementDetail {
  id: string;
  requestNumber: string;
  title: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  currency: string;
  expenseDate: string;
  description: string;
  costCenterId: string;
  costCenterCode: string;
  status: RequestStatus;
  createdByUserId: string;
  createdByUserName: string;
  approvedByUserId?: string | null;
  paidByUserId?: string | null;
  rejectionReason?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  paidAt?: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
  allowedActions: ReimbursementAllowedActions;
  attachments: Attachment[];
  workflowActions: WorkflowAction[];
}

export interface ReimbursementListQuery {
  page?: number;
  pageSize?: number;
  status?: RequestStatus;
  categoryId?: string;
  costCenterId?: string;
  expenseDateFrom?: string;
  expenseDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  requestNumber?: string;
  createdByMe?: boolean;
  sort?: string;
}

export type ReimbursementListResponse = PagedResult<ReimbursementListItem>;

export interface CreateReimbursementPayload {
  title: string;
  categoryId: string;
  amount: number;
  currency: string;
  expenseDate: string;
  description: string;
}

export interface UpdateReimbursementDraftPayload extends CreateReimbursementPayload {
  rowVersion: string;
}
