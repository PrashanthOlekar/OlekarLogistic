/**
 * The signed-in user and their token, kept in the browser's localStorage
 * so a page refresh doesn't sign them out.
 */
import type { SessionUser } from './types';

const TOKEN_KEY = 'procargo.token';
const USER_KEY = 'procargo.user';

export const session = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  getUser(): SessionUser | null {
    try {
      return JSON.parse(localStorage.getItem(USER_KEY) ?? 'null');
    } catch {
      return null;
    }
  },

  save(token: string, user: SessionUser): void {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  saveUser(user: SessionUser): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  clear(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
};
