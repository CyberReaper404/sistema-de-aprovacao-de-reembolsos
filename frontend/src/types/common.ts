export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}

export interface SelectOption<TValue extends string = string> {
  value: TValue;
  label: string;
}

export interface DownloadedFile {
  content: Blob;
  fileName?: string;
  contentType: string;
}
