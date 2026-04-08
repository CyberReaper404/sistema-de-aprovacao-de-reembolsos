import type { ProblemDetails } from "@/types/common";

export class ApiError extends Error {
  public readonly status: number;
  public readonly problemDetails?: ProblemDetails;

  constructor(message: string, status: number, problemDetails?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problemDetails = problemDetails;
  }
}
