import { ApiError } from "@/services/http/api-error";
import type { DownloadedFile, ProblemDetails } from "@/types/common";

export interface HttpClientOptions {
  baseUrl?: string;
}

type QueryValue = string | number | boolean | undefined | null;
type QueryObject = object;
type AccessTokenResolver = () => string | null;
type UnauthorizedHandler = () => Promise<boolean>;

export class HttpClient {
  private readonly baseUrl: string;
  private getAccessToken: AccessTokenResolver;
  private onUnauthorized: UnauthorizedHandler | null;

  constructor(options: HttpClientOptions = {}) {
    this.baseUrl = options.baseUrl ?? "/api";
    this.getAccessToken = () => null;
    this.onUnauthorized = null;
  }

  public setAccessTokenResolver(resolver?: AccessTokenResolver) {
    this.getAccessToken = resolver ?? (() => null);
  }

  public setUnauthorizedHandler(handler?: UnauthorizedHandler) {
    this.onUnauthorized = handler ?? null;
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

  public download(path: string) {
    return this.requestFile(path, { method: "GET" });
  }

  private async request<TResponse>(
    path: string,
    init: {
      method: string;
      body?: unknown;
      query?: QueryObject;
    },
    allowRetry = true
  ): Promise<TResponse> {
    const url = this.buildUrl(path, init.query);
    const headers = new Headers();
    const accessToken = this.getAccessToken();

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
      if (response.status === 401 && allowRetry && this.onUnauthorized && this.shouldRetryUnauthorized(path)) {
        const recovered = await this.onUnauthorized();

        if (recovered) {
          return this.request<TResponse>(path, init, false);
        }
      }

      throw await this.toApiError(response);
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    return (await response.json()) as TResponse;
  }

  private async requestFile(
    path: string,
    init: {
      method: string;
    },
    allowRetry = true
  ): Promise<DownloadedFile> {
    const url = this.buildUrl(path);
    const headers = new Headers();
    const accessToken = this.getAccessToken();

    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    const response = await fetch(url, {
      method: init.method,
      headers,
      credentials: "include"
    });

    if (!response.ok) {
      if (response.status === 401 && allowRetry && this.onUnauthorized && this.shouldRetryUnauthorized(path)) {
        const recovered = await this.onUnauthorized();

        if (recovered) {
          return this.requestFile(path, init, false);
        }
      }

      throw await this.toApiError(response);
    }

    const content = await response.blob();
    const contentDisposition = response.headers.get("Content-Disposition");
    const encodedFileName = contentDisposition?.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
    const plainFileName = contentDisposition?.match(/filename="?([^"]+)"?/i)?.[1];
    const fileName = encodedFileName ? decodeURIComponent(encodedFileName) : plainFileName;

    return {
      content,
      fileName,
      contentType: response.headers.get("Content-Type") ?? content.type ?? "application/octet-stream"
    };
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

  private shouldRetryUnauthorized(path: string) {
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;

    return !(
      normalizedPath.startsWith("/auth/login") ||
      normalizedPath.startsWith("/auth/refresh") ||
      normalizedPath.startsWith("/auth/logout")
    );
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
