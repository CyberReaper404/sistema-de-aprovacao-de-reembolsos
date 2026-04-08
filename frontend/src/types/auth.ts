import type { UserRole } from "@/types/domain";

export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  primaryCostCenterId: string;
}

export interface AuthSession {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: AuthenticatedUser;
}

export interface LoginPayload {
  email: string;
  password: string;
}
