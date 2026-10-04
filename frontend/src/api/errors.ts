/**
 * Turns any failed request into an ApiError with a message that can be shown to the user.
 * The API answers errors with RFC 7807 ProblemDetails:
 *   { type, title, status, detail, traceId, errors?: { field: [messages] } }
 */
import { isAxiosError } from 'axios';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly title?: string;
  readonly traceId?: string;
  /** Validation messages per field (400 responses), e.g. { mobile: ['Enter a 10-digit mobile number.'] }. */
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.title = problem?.title;
    this.traceId = problem?.traceId;
    this.fieldErrors = problem?.errors ?? {};
  }
}

const NETWORK_MESSAGE = 'Cannot reach the ProCargo server. Check your internet connection and try again.';
const FALLBACK_MESSAGES: Record<number, string> = {
  400: 'Please check the details and try again.',
  401: 'Your session has ended. Please sign in again.',
  403: "You don't have access to this.",
  404: 'Not found.',
  409: 'This was changed by someone else. Refresh and try again.',
  413: 'The file is too large.',
  429: 'Too many attempts. Wait a minute and try again.',
};

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }
  if (!isAxiosError(error)) {
    return new ApiError(0, 'Something went wrong.');
  }
  if (!error.response) {
    return new ApiError(0, error.code === 'ECONNABORTED' ? 'The server took too long to answer.' : NETWORK_MESSAGE);
  }

  const status = error.response.status;
  const problem = asProblem(error.response.data);
  return new ApiError(status, messageFor(status, problem), problem);
}

/** Like toApiError, but for responses read as a Blob (file downloads). */
export async function toApiErrorFromBlob(error: unknown): Promise<ApiError> {
  if (isAxiosError(error) && error.response?.data instanceof Blob) {
    try {
      error.response.data = JSON.parse(await error.response.data.text());
    } catch {
      error.response.data = undefined;
    }
  }
  return toApiError(error);
}

function asProblem(data: unknown): ProblemDetails | undefined {
  return data && typeof data === 'object' ? (data as ProblemDetails) : undefined;
}

function messageFor(status: number, problem?: ProblemDetails): string {
  if (problem?.detail) {
    return problem.detail;
  }

  // Validation errors: show the first message for each field.
  const fieldMessages = Object.values(problem?.errors ?? {})
    .map((messages) => messages[0])
    .filter(Boolean);
  if (fieldMessages.length > 0) {
    return fieldMessages.slice(0, 3).join(' ');
  }

  if (status >= 500) {
    const reference = problem?.traceId ? ` (reference ${problem.traceId})` : '';
    return `Something went wrong on our side. Please try again${reference}.`;
  }
  return FALLBACK_MESSAGES[status] ?? problem?.title ?? `Request failed (${status}).`;
}
