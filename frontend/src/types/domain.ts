export type UserRole = "Collaborator" | "Manager" | "Finance" | "Administrator";

export type RequestStatus = "Draft" | "Submitted" | "Approved" | "Rejected" | "Paid";

export type WorkflowActionType =
  | "DraftCreated"
  | "DraftUpdated"
  | "Submitted"
  | "Approved"
  | "Rejected"
  | "PaymentRecorded"
  | "AttachmentAdded"
  | "AttachmentDeleted";

export type PaymentMethod = "BankTransfer" | "Pix" | "Cash" | "Other";

export type DashboardPeriodGrouping = "Day" | "Month";
