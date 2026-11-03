import { HttpErrorResponse } from '@angular/common/http';

/** Shape of an RFC 9457 problem details payload returned by the API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
}

/** Normalised error raised by every HTTP call, so components never deal with HttpErrorResponse. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Readonly<Record<string, string[]>> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }

  get isValidation(): boolean {
    return this.status === 400 && Object.keys(this.fieldErrors).length > 0;
  }

  static fromHttpError(error: HttpErrorResponse): ApiError {
    if (error.status === 0) {
      return new ApiError(0, 'The server could not be reached. Please check that the API is running.');
    }

    const problem = isProblemDetails(error.error) ? error.error : undefined;
    const message = problem?.detail ?? problem?.title ?? `Request failed with status ${error.status}.`;

    return new ApiError(error.status, message, problem?.errors ?? {});
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value);
}
