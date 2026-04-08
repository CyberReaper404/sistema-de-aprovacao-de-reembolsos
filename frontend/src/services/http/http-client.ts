import { ApiError } from "@/services/http/api-error";
import type { ProblemDetails } from "@/types/common";

export interface HttpClientOptions {
  baseUrl?: string;
  getAccessToken?: () => string | null;
}

type QueryValue = string | number | boolean | undefined | null;
type QueryObject = object;

export class HttpClient {
  private readonly baseUrl: string;
  private readonly getAccessToken?: () => string | null;

  constructor(options: HttpClientOptions = {}) {
    this.baseUrl = options.baseUrl ?? "/api";
    this.getAccessToken = options.getAccessToken;
  }

  public get<TResponse>(path: string, query?: QueryObject) {
    return this.request<TResponse>(path, { method: "GET", query });
  }

  public post<TResponse>(path: string, body?: unknown) {
    return this.request<TResponse>(path, { method: "POST", body });
  }

  public put<TResponse>(path: string, body?: unknown) {
    return this.request<TResponse>(path, { method: "PUT", body });
  }

  public delete<TResponse>(path: string) {
    return this.request<TResponse>(path, { method: "DELETE" });
  }

  private async request<TResponse>(
    path: string,
    init: {
      method: string;
      body?: unknown;
      query?: QueryObject;
    }
  ): Promise<TResponse> {
    const url = this.buildUrl(path, init.query);
    const headers = new Headers();
    const accessToken = this.getAccessToken?.();

    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    if (init.body !== undefined && !(init.body instanceof FormData)) {
      headers.set("Content-Type", "application/json");
    }

    const response = await fetch(url, {
      method: init.method,
      headers,
      credentials: "include",
      body:
        init.body === undefined
          ? undefined
          : init.body instanceof FormData
            ? init.body
            : JSON.stringify(init.body)
    });

    if (!response.ok) {
      throw await this.toApiError(response);
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    return (await response.json()) as TResponse;
  }

  private buildUrl(path: string, query?: QueryObject) {
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    const url = new URL(`${this.baseUrl}${normalizedPath}`, window.location.origin);

    if (query) {
      Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== "") {
          url.searchParams.set(key, String(value as QueryValue));
        }
      });
    }

    return `${url.pathname}${url.search}`;
  }

  private async toApiError(response: Response) {
    let problemDetails: ProblemDetails | undefined;

    try {
      problemDetails = (await response.json()) as ProblemDetails;
    } catch {
      problemDetails = undefined;
    }

    const message = problemDetails?.detail ?? problemDetails?.title ?? "Não foi possível concluir a solicitação.";

    return new ApiError(message, response.status, problemDetails);
  }
}
