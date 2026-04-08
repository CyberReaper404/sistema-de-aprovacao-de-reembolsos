import { HttpClient } from "@/services/http/http-client";
import type { PaymentDetail, PaymentListQuery, PaymentListResponse } from "@/types/payments";

export class PaymentService {
  constructor(private readonly httpClient: HttpClient) {}

  public getPaged(query: PaymentListQuery) {
    return this.httpClient.get<PaymentListResponse>("/payments", query);
  }

  public getById(id: string) {
    return this.httpClient.get<PaymentDetail>(`/payments/${id}`);
  }
}
