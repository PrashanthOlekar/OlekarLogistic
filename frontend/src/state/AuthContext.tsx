/** Who is signed in. Wrap the app in <AuthProvider> and read it with useAuth(). */
import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { SIGNED_OUT_EVENT } from '../lib/api';
import { session } from '../lib/session';
import type { SessionUser } from '../lib/types';

interface AuthState {
  user: SessionUser | null;
  signIn: (token: string, user: SessionUser) => void;
  setUser: (user: SessionUser) => void;
  signOut: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<SessionUser | null>(session.getUser());

  // The API helper fires this event when the server rejects the token.
  useEffect(() => {
    const handleSignedOut = () => setUserState(null);
    window.addEventListener(SIGNED_OUT_EVENT, handleSignedOut);
    return () => window.removeEventListener(SIGNED_OUT_EVENT, handleSignedOut);
  }, []);

  const signIn = useCallback((token: string, signedInUser: SessionUser) => {
    session.save(token, signedInUser);
    setUserState(signedInUser);
  }, []);

  const setUser = useCallback((updatedUser: SessionUser) => {
    session.saveUser(updatedUser);
    setUserState(updatedUser);
  }, []);

  const signOut = useCallback(() => {
    session.clear();
    setUserState(null);
  }, []);

  return <AuthContext.Provider value={{ user, signIn, setUser, signOut }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return state;
}
