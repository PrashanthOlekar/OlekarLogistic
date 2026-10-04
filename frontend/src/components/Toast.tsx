/** Short pop-up messages in the corner: const toast = useToast(); toast('Saved.'); */
import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react';

type ToastKind = 'ok' | 'error';
type ShowToast = (text: string, kind?: ToastKind) => void;

interface Toast {
  id: number;
  text: string;
  kind: ToastKind;
}

const TOAST_DURATION_MS = 3800;

const ToastContext = createContext<ShowToast>(() => {});

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(1);

  const showToast = useCallback<ShowToast>((text, kind = 'ok') => {
    const id = nextId.current++;
    setToasts((current) => [...current, { id, text, kind }]);
    setTimeout(() => setToasts((current) => current.filter((toast) => toast.id !== id)), TOAST_DURATION_MS);
  }, []);

  return (
    <ToastContext.Provider value={showToast}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={toast.kind === 'error' ? 'toast error' : 'toast'}>
            {toast.text}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ShowToast {
  return useContext(ToastContext);
}
