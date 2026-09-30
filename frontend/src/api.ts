// Thin wrapper around fetch for the Olekar API. Every call sends the saved sign-in token.

const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? '';
const TOKEN_KEY = 'olk.token';
const USER_KEY = 'olk.user';

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export type Role = 'Customer' | 'Owner' | 'Driver' | 'Admin';
export interface SessionUser {
  id: number;
  role: Role;
  fullName: string;
  mobile: string;
  email?: string | null;
  status: string;
  detail?: Record<string, unknown> | null;
}

export const session = {
  token: () => localStorage.getItem(TOKEN_KEY),
  user: (): SessionUser | null => {
    try { return JSON.parse(localStorage.getItem(USER_KEY) ?? 'null'); } catch { return null; }
  },
  save(token: string, user: SessionUser) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  saveUser(user: SessionUser) { localStorage.setItem(USER_KEY, JSON.stringify(user)); },
  clear() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }
};

type Options = { method?: string; body?: unknown; form?: FormData };

export async function api<T = unknown>(path: string, opts: Options = {}): Promise<T> {
  const headers: Record<string, string> = {};
  const token = session.token();
  if (token) headers.Authorization = `Bearer ${token}`;
  let body: BodyInit | undefined;
  if (opts.form) body = opts.form;
  else if (opts.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(opts.body);
  }
  let res: Response;
  try {
    res = await fetch(`${BASE}/api${path}`, { method: opts.method ?? (body ? 'POST' : 'GET'), headers, body });
  } catch {
    throw new ApiError(0, 'Cannot reach the Olekar server. Check your internet connection and try again.');
  }
  if (res.status === 401) {
    session.clear();
    window.dispatchEvent(new Event('olk:signed-out'));
    throw new ApiError(401, 'Your session has ended. Please sign in again.');
  }
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  const data = text ? safeJson(text) : undefined;
  if (!res.ok) {
    const msg = (data && typeof data === 'object' && 'error' in data && typeof data.error === 'string')
      ? data.error
      : `Request failed (${res.status}).`;
    throw new ApiError(res.status, msg);
  }
  return data as T;
}

function safeJson(text: string): unknown {
  try { return JSON.parse(text); } catch { return text; }
}

/** Opens an uploaded document in a new tab (the file endpoint needs the sign-in token, so it is fetched first). */
export async function openDocument(id: number) {
  const token = session.token();
  const res = await fetch(`${BASE}/api/documents/${id}/file`, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
  if (!res.ok) throw new ApiError(res.status, 'Could not open this document.');
  const url = URL.createObjectURL(await res.blob());
  window.open(url, '_blank', 'noopener');
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

// ---------- shared types ----------
export interface Meta {
  cities: { id: number; name: string; nameKn?: string | null; state: string }[];
  vehicleTypes: { id: number; code: string; name: string; bodyType: string; maxLoadKg: number; lengthFt: number; widthFt: number; heightFt?: number | null; recommendedGoods?: string | null }[];
  goods: { id: number; name: string }[];
}

let metaCache: Promise<Meta> | null = null;
export function getMeta() {
  metaCache ??= api<Meta>('/meta').catch((e) => { metaCache = null; throw e; });
  return metaCache;
}
