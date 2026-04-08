import type { UserRole } from "@/types/domain";

export interface NavigationItem {
  label: string;
  to: string;
  roles: UserRole[];
}
