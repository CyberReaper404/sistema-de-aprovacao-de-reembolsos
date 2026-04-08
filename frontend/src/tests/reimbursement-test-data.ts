import { PaymentMethod, RequestStatus, WorkflowActionType } from "@/types/domain";
import type { AuthSession } from "@/types/auth";
import type { ReimbursementCategoryOption, ReimbursementDetail } from "@/types/reimbursements";

export function createSession(overrides: Partial<AuthSession> = {}): AuthSession {
  return {
    accessToken: "token-teste",
    accessTokenExpiresAt: "2026-04-08T18:00:00Z",
    refreshTokenExpiresAt: "2026-04-09T18:00:00Z",
    user: {
      id: "usuario-1",
      fullName: "Maria Oliveira",
      email: "maria@empresa.com",
      role: 1,
      primaryCostCenterId: "cc-01"
    },
    ...overrides
  };
}

export function createCategory(overrides: Partial<ReimbursementCategoryOption> = {}): ReimbursementCategoryOption {
  return {
    id: "categoria-1",
    name: "Transporte",
    description: "Despesas de deslocamento urbano.",
    maxAmount: 500,
    receiptRequiredAboveAmount: 50,
    ...overrides
  };
}

export function createReimbursementDetail(overrides: Partial<ReimbursementDetail> = {}): ReimbursementDetail {
  return {
    id: "solicitacao-1",
    requestNumber: "RB-2026-0001",
    title: "Taxi para visita ao cliente",
    categoryId: "categoria-1",
    categoryName: "Transporte",
    amount: 125.4,
    currency: "BRL",
    expenseDate: "2026-04-05",
    description: "Corrida entre o escritorio e a reuniao externa.",
    costCenterId: "cc-01",
    costCenterCode: "COM-001",
    status: RequestStatus.Draft,
    createdByUserId: "usuario-1",
    createdByUserName: "Maria Oliveira",
    approvedByUserId: null,
    paidByUserId: null,
    rejectionReason: null,
    submittedAt: null,
    approvedAt: null,
    rejectedAt: null,
    paidAt: null,
    createdAt: "2026-04-05T14:20:00Z",
    updatedAt: "2026-04-05T14:20:00Z",
    rowVersion: "AAAAAAABAAA=",
    allowedActions: {
      canEditDraft: true,
      canSubmit: true,
      canApprove: false,
      canReject: false,
      canRecordPayment: false,
      canUploadAttachment: true,
      canDeleteAttachment: true
    },
    attachments: [],
    workflowActions: [
      {
        id: "workflow-1",
        actionType: WorkflowActionType.DraftCreated,
        fromStatus: null,
        toStatus: RequestStatus.Draft,
        performedByUserId: "usuario-1",
        comment: null,
        occurredAt: "2026-04-05T14:20:00Z"
      }
    ],
    ...overrides
  };
}

export const paymentMethodExample = PaymentMethod.BankTransfer;
