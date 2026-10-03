/**
 * Talks to the ProCargo API (ASP.NET Core). Every request sends the saved sign-in token.
 *
 *   const bookings = await api<BookingRow[]>('/bookings');
 *   await api('/bookings/12/pay', { body: { method: 'UPI' } });
 */
import { session } from './session';

const API_BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? '';

/** Fired when the server says the token is no longer valid; the sign-in state listens for it. */
export const SIGNED_OUT_EVENT = 'procargo:signed-out';

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

interface RequestOptions {
  method?: string;
  /** Sent as JSON. */
  body?: unknown;
  /** Sent as multipart form data (file uploads). */
  form?: FormData;
}

export async function api<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {};
  const token = session.getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let body: BodyInit | undefined;
  if (options.form) {
    body = options.form;
  } else if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(options.body);
  }

  const method = options.method ?? (body ? 'POST' : 'GET');

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api${path}`, { method, headers, body });
  } catch {
    throw new ApiError(0, 'Cannot reach the ProCargo server. Check your internet connection and try again.');
  }

  if (response.status === 401) {
    session.clear();
    window.dispatchEvent(new Event(SIGNED_OUT_EVENT));
    throw new ApiError(401, 'Your session has ended. Please sign in again.');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const data = text ? parseJson(text) : undefined;

  if (!response.ok) {
    throw new ApiError(response.status, readErrorMessage(data) ?? `Request failed (${response.status}).`);
  }

  return data as T;
}

/**
 * Opens an uploaded document in a new tab.
 * The file endpoint needs the sign-in token, so the file is downloaded first.
 */
export async function openDocument(documentId: number): Promise<void> {
  const token = session.getToken();
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/file`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new ApiError(response.status, 'Could not open this document.');
  }

  const url = URL.createObjectURL(await response.blob());
  window.open(url, '_blank', 'noopener');
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

function parseJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

/** The API sends errors as { "error": "Friendly message" }. */
function readErrorMessage(data: unknown): string | null {
  if (data && typeof data === 'object' && 'error' in data && typeof data.error === 'string') {
    return data.error;
  }
  return null;
}
