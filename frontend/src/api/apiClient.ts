/**
 * The one Axios instance every API module uses.
 *
 * - Base URL comes from VITE_API_BASE_URL, e.g. https://localhost:7001/api/v1.
 * - Adds the access token to every request, refreshing it first when it is about to expire.
 * - On 401 it refreshes once and retries; if that fails the user is signed out.
 * - On 403 "Account not active" (blocked by an admin) the user is signed out.
 * - Every error is rethrown as an ApiError with a user-friendly message.
 */
import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { emitSessionEvent, type SessionEndReason } from '../auth/sessionEvents';
import { tokenStorage } from '../auth/tokenStorage';
import type { AuthResponse } from '../types';
import { toApiError, type ProblemDetails } from './errors';

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Send without the access token and never try to refresh (sign-in, refresh, public lists). */
    skipAuth?: boolean;
    /** Set internally after the one retry that follows a refresh. */
    _retried?: boolean;
  }
}

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/+$/, '');

const REQUEST_TIMEOUT_MS = 30_000;

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: REQUEST_TIMEOUT_MS,
  headers: { Accept: 'application/json' },
});

/** A separate instance for the refresh call, so it never passes through the interceptors below. */
const refreshClient = axios.create({ baseURL: API_BASE_URL, timeout: REQUEST_TIMEOUT_MS });

let refreshInFlight: Promise<boolean> | null = null;

/**
 * Exchanges the refresh token for a new access token. Concurrent callers share one request,
 * because the API rotates refresh tokens and treats a reused one as theft.
 * Resolves false (and signs out) when the session can't be renewed.
 */
export function refreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) {
      return false;
    }
    try {
      const { data } = await refreshClient.post<AuthResponse>('/auth/refresh', { refreshToken });
      storeSession(data);
      emitSessionEvent({ type: 'refreshed', user: data.user });
      return true;
    } catch (error) {
      // A network failure is not a reason to throw away the session; anything the server rejected is.
      if (axios.isAxiosError(error) && !error.response) {
        throw toApiError(error);
      }
      endSession('expired');
      return false;
    }
  })().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

/** Saves the tokens from a sign-in, registration or refresh response. */
export function storeSession(response: AuthResponse): void {
  tokenStorage.setAccessToken(response.accessToken, response.expiresIn);
  tokenStorage.setRefreshToken(response.refreshToken);
  tokenStorage.setUser(response.user);
}

export function endSession(reason: SessionEndReason): void {
  const hadSession = tokenStorage.getRefreshToken() !== null || tokenStorage.getAccessToken() !== null;
  tokenStorage.clear();
  if (hadSession) {
    emitSessionEvent({ type: 'ended', reason });
  }
}

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  if (config.skipAuth) {
    return config;
  }
  if (tokenStorage.accessTokenExpiresSoon() && tokenStorage.getRefreshToken()) {
    await refreshSession();
  }
  const token = tokenStorage.getAccessToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const config = error.config;
    const status = error.response?.status;

    if (status === 401 && config && !config.skipAuth) {
      if (!config._retried && tokenStorage.getRefreshToken() && (await refreshSession())) {
        config._retried = true;
        return apiClient.request(config);
      }
      endSession('expired');
    }

    // A blocked or closed account is refused even with a valid token.
    if (status === 403 && error.response?.data?.title === 'Account not active') {
      endSession('account-inactive');
    }

    throw toApiError(error);
  },
);

/** Removes empty filters so the query string only carries what the user chose. */
export function cleanParams<T extends object>(query: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  ) as Partial<T>;
}
