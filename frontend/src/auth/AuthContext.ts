import { createContext } from 'react';
import type { SessionEndReason } from './sessionEvents';
import type { AuthResponse, Role, UserProfile } from '../types';

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface AuthState {
  /** 'loading' while a saved session is being renewed on start-up. */
  status: AuthStatus;
  user: UserProfile | null;
  /** Why the last session ended, so the sign-in page can explain it. Cleared on the next sign-in. */
  endReason: SessionEndReason | null;
  /** Stores the tokens from a sign-in or registration response. */
  signIn: (response: AuthResponse) => void;
  /** Revokes the refresh token on the server and forgets the session here. */
  signOut: () => Promise<void>;
  /** Reloads the profile, for example after KYC status changes. */
  refreshProfile: () => Promise<void>;
  hasRole: (...roles: Role[]) => boolean;
}

export const AuthContext = createContext<AuthState | null>(null);
