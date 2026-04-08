import type { PagedResult } from "@/types/common";
import type { PaymentMethod } from "@/types/domain";

export interface PaymentListItem {
  id: string;
  requestId: string;
  requestNumber: string;
  requestTitle: string;
  costCenterId: string;
  costCenterCode: string;
  categoryId: string;
  categoryName: string;
  amountPaid: number;
  currency: string;
  paymentMethod: PaymentMethod;
  paymentReference: string;
  paidByUserId: string;
  paidAt: string;
  createdAt: string;
}

export interface PaymentDetail extends PaymentListItem {
  notes?: string | null;
}

export interface PaymentListQuery {
  page?: number;
  pageSize?: number;
  costCenterId?: string;
  paymentMethod?: PaymentMethod;
  paidFrom?: string;
  paidTo?: string;
  requestNumber?: string;
  sort?: string;
}

export type PaymentListResponse = PagedResult<PaymentListItem>;
