/** Who is signed in. Wrap the app in <AuthProvider> and read it with useAuth(). */
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/authApi';
import { endSession, refreshSession, storeSession } from '../api/apiClient';
import type { AuthResponse, Role, UserProfile } from '../types';
import { AuthContext, type AuthState, type AuthStatus } from './AuthContext';
import { onSessionEvent, type SessionEndReason } from './sessionEvents';
import { tokenStorage } from './tokenStorage';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(() => tokenStorage.getUser());
  const [status, setStatus] = useState<AuthStatus>(() =>
    tokenStorage.getRefreshToken() ? 'loading' : 'anonymous',
  );
  const [endReason, setEndReason] = useState<SessionEndReason | null>(null);

  // The API client reports refreshed tokens and ended sessions (expired, blocked) through these events.
  useEffect(
    () =>
      onSessionEvent((event) => {
        if (event.type === 'refreshed') {
          setUser(event.user);
          setStatus('authenticated');
        } else {
          setUser(null);
          setStatus('anonymous');
          setEndReason(event.reason);
        }
      }),
    [],
  );

  // On start-up, swap the saved refresh token for a fresh access token.
  useEffect(() => {
    tokenStorage.removeLegacyKeys();
    if (!tokenStorage.getRefreshToken()) {
      tokenStorage.clear();
      setUser(null);
      setStatus('anonymous');
      return;
    }
    refreshSession()
      .then((renewed) => setStatus(renewed ? 'authenticated' : 'anonymous'))
      // Offline: keep the saved user; requests will retry the refresh when the network is back.
      .catch(() => setStatus(tokenStorage.getUser() ? 'authenticated' : 'anonymous'));
  }, []);

  const signIn = useCallback((response: AuthResponse) => {
    storeSession(response);
    setUser(response.user);
    setStatus('authenticated');
    setEndReason(null);
  }, []);

  const signOut = useCallback(async () => {
    try {
      await authApi.logout(tokenStorage.getRefreshToken());
    } catch {
      // Signing out here must always work, even if the server can't be reached.
    }
    endSession('signed-out');
    setUser(null);
    setStatus('anonymous');
  }, []);

  const refreshProfile = useCallback(async () => {
    const profile = await authApi.getProfile();
    tokenStorage.setUser(profile);
    setUser(profile);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      status,
      user,
      endReason,
      signIn,
      signOut,
      refreshProfile,
      hasRole: (...roles: Role[]) => !!user && roles.includes(user.role),
    }),
    [status, user, endReason, signIn, signOut, refreshProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
