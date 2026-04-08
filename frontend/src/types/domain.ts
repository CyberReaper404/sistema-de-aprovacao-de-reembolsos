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
  PaymentRegistered = 8
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
