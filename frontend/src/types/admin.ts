import type { PagedResult } from "@/types/common";
import type { AuditSeverity, UserRole } from "@/types/domain";

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  primaryCostCenterId: string;
  isActive: boolean;
  managedCostCenterIds: string[];
}

export interface AdminCategory {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  maxAmount?: number | null;
  receiptRequiredAboveAmount?: number | null;
  receiptRequiredAlways: boolean;
  submissionDeadlineDays?: number | null;
}

export interface AdminCostCenter {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface AdminAuditEntry {
  id: string;
  eventType: string;
  entityType: string;
  entityId?: string | null;
  actorUserId?: string | null;
  severity: AuditSeverity;
  occurredAt: string;
  metadataJson?: string | null;
}

export type AdminUserListResponse = PagedResult<AdminUser>;
export type AdminAuditListResponse = PagedResult<AdminAuditEntry>;
