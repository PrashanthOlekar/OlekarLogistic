/**
 * Where the sign-in lives in the browser.
 *
 * - The access token (30 minutes) is kept in memory only, so other scripts on the page can't read it
 *   from storage and it disappears when the tab closes.
 * - The refresh token and the user's profile are kept in localStorage so a page refresh doesn't sign
 *   the user out; on start-up the refresh token is exchanged for a new access token.
 */
import type { UserProfile } from '../types';

const REFRESH_TOKEN_KEY = 'procargo.refreshToken';
const USER_KEY = 'procargo.user';
/** Left behind by the previous portal version; removed on start-up. */
const LEGACY_KEYS = ['procargo.token'];

let accessToken: string | null = null;
let accessTokenExpiresAt = 0;

export const tokenStorage = {
  getAccessToken(): string | null {
    return accessToken;
  },

  /** True when there is no access token or it expires within `marginMs`. */
  accessTokenExpiresSoon(marginMs = 30_000): boolean {
    return !accessToken || Date.now() + marginMs >= accessTokenExpiresAt;
  },

  setAccessToken(token: string, expiresInSeconds: number): void {
    accessToken = token;
    accessTokenExpiresAt = Date.now() + expiresInSeconds * 1000;
  },

  getRefreshToken(): string | null {
    return read(REFRESH_TOKEN_KEY);
  },

  setRefreshToken(token: string): void {
    write(REFRESH_TOKEN_KEY, token);
  },

  getUser(): UserProfile | null {
    try {
      return JSON.parse(read(USER_KEY) ?? 'null');
    } catch {
      return null;
    }
  },

  setUser(user: UserProfile): void {
    write(USER_KEY, JSON.stringify(user));
  },

  clear(): void {
    accessToken = null;
    accessTokenExpiresAt = 0;
    remove(REFRESH_TOKEN_KEY);
    remove(USER_KEY);
  },

  removeLegacyKeys(): void {
    LEGACY_KEYS.forEach(remove);
  },
};

// Storage can be unavailable (private mode, blocked site data); the app then works for this tab only.
function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* keep going without persistence */
  }
}

function remove(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch {
    /* nothing to remove */
  }
}
