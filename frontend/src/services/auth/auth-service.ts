import { HttpClient } from "@/services/http/http-client";
import type { AuthSession, LoginPayload } from "@/types/auth";

export class AuthService {
  constructor(private readonly httpClient: HttpClient) {}

  public login(payload: LoginPayload) {
    return this.httpClient.post<AuthSession>("/auth/login", payload);
  }

  public refresh() {
    return this.httpClient.post<AuthSession>("/auth/refresh");
  }

  public me() {
    return this.httpClient.get<AuthSession["user"]>("/auth/me");
  }

  public logout() {
    return this.httpClient.post<void>("/auth/logout");
  }

  public logoutAll() {
    return this.httpClient.post<void>("/auth/logout-all");
  }
}
