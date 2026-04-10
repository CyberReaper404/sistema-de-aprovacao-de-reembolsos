export enum UserRole {
  Collaborator = 1,
  Manager = 2,
  Finance = 3,
  Administrator = 4
}

export enum RequestStatus {
  Draft = 1,
  Submitted = 2,
  Approved = 3,
  Rejected = 4,
  Paid = 5
}

export enum WorkflowActionType {
  DraftCreated = 1,
  DraftUpdated = 2,
  Submitted = 3,
  Approved = 4,
  Rejected = 5,
  AttachmentAdded = 6,
  AttachmentRemoved = 7,
  PaymentRegistered = 8,
  ComplementationRequested = 9
}

export enum PaymentMethod {
  BankTransfer = 1,
  Pix = 2,
  Other = 3
}

export enum DashboardPeriodGrouping {
  Day = 1,
  Month = 2
}

export enum AuditSeverity {
  Information = 1,
  Warning = 2,
  Critical = 3
}

export enum DecisionReasonCode {
  MissingReceipt = 1,
  InvalidReceipt = 2,
  OutOfPolicy = 3,
  OutOfDeadline = 4,
  CategoryMismatch = 5,
  DuplicateRequest = 6,
  InconsistentAmount = 7,
  FraudSuspicion = 8,
  NeedMoreDetails = 9,
  NeedAdditionalDocument = 10,
  Other = 11
}

export const userRoleLabels: Record<UserRole, string> = {
  [UserRole.Collaborator]: "Colaborador",
  [UserRole.Manager]: "Gestor",
  [UserRole.Finance]: "Financeiro",
  [UserRole.Administrator]: "Administrador"
};

export const requestStatusLabels: Record<RequestStatus, string> = {
  [RequestStatus.Draft]: "Rascunho",
  [RequestStatus.Submitted]: "Em análise",
  [RequestStatus.Approved]: "Aprovada",
  [RequestStatus.Rejected]: "Recusada",
  [RequestStatus.Paid]: "Paga"
};

export const workflowActionTypeLabels: Record<WorkflowActionType, string> = {
  [WorkflowActionType.DraftCreated]: "Rascunho criado",
  [WorkflowActionType.DraftUpdated]: "Rascunho atualizado",
  [WorkflowActionType.Submitted]: "Solicitação enviada",
  [WorkflowActionType.Approved]: "Solicitação aprovada",
  [WorkflowActionType.Rejected]: "Solicitação recusada",
  [WorkflowActionType.AttachmentAdded]: "Anexo incluído",
  [WorkflowActionType.AttachmentRemoved]: "Anexo removido",
  [WorkflowActionType.PaymentRegistered]: "Pagamento registrado",
  [WorkflowActionType.ComplementationRequested]: "Complementação solicitada"
};

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.BankTransfer]: "Transferência bancária",
  [PaymentMethod.Pix]: "Pix",
  [PaymentMethod.Other]: "Outro"
};

export const auditSeverityLabels: Record<AuditSeverity, string> = {
  [AuditSeverity.Information]: "Informativo",
  [AuditSeverity.Warning]: "Atenção",
  [AuditSeverity.Critical]: "Crítico"
};

export const decisionReasonLabels: Record<DecisionReasonCode, string> = {
  [DecisionReasonCode.MissingReceipt]: "Comprovante ausente",
  [DecisionReasonCode.InvalidReceipt]: "Comprovante inválido",
  [DecisionReasonCode.OutOfPolicy]: "Fora da política",
  [DecisionReasonCode.OutOfDeadline]: "Fora do prazo",
  [DecisionReasonCode.CategoryMismatch]: "Categoria incompatível",
  [DecisionReasonCode.DuplicateRequest]: "Solicitação duplicada",
  [DecisionReasonCode.InconsistentAmount]: "Valor inconsistente",
  [DecisionReasonCode.FraudSuspicion]: "Suspeita de fraude",
  [DecisionReasonCode.NeedMoreDetails]: "Mais detalhes necessários",
  [DecisionReasonCode.NeedAdditionalDocument]: "Documento complementar necessário",
  [DecisionReasonCode.Other]: "Outro"
};
