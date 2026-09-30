import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { session, type SessionUser } from './api';

// ---------------- sign-in state ----------------
interface AuthState {
  user: SessionUser | null;
  signIn: (token: string, user: SessionUser) => void;
  setUser: (user: SessionUser) => void;
  signOut: () => void;
}
const AuthCtx = createContext<AuthState>(null!);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<SessionUser | null>(session.user());
  useEffect(() => {
    const off = () => setUserState(null);
    window.addEventListener('olk:signed-out', off);
    return () => window.removeEventListener('olk:signed-out', off);
  }, []);
  const signIn = useCallback((token: string, u: SessionUser) => { session.save(token, u); setUserState(u); }, []);
  const setUser = useCallback((u: SessionUser) => { session.saveUser(u); setUserState(u); }, []);
  const signOut = useCallback(() => { session.clear(); setUserState(null); }, []);
  return <AuthCtx.Provider value={{ user, signIn, setUser, signOut }}>{children}</AuthCtx.Provider>;
}
export const useAuth = () => useContext(AuthCtx);

// ---------------- toasts ----------------
type Toast = { id: number; text: string; kind: 'ok' | 'error' };
const ToastCtx = createContext<(text: string, kind?: 'ok' | 'error') => void>(() => {});

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const next = useRef(1);
  const push = useCallback((text: string, kind: 'ok' | 'error' = 'ok') => {
    const id = next.current++;
    setToasts((t) => [...t, { id, text, kind }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3800);
  }, []);
  return (
    <ToastCtx.Provider value={push}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => <div key={t.id} className={`toast ${t.kind === 'error' ? 'error' : ''}`}>{t.text}</div>)}
      </div>
    </ToastCtx.Provider>
  );
}
export const useToast = () => useContext(ToastCtx);
