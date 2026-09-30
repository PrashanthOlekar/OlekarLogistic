import { useCallback, useEffect, useState, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { ApiError } from '../api';

// ---------------- formatting ----------------
export const inr = (n?: number | null) => {
  if (n == null) return '—';
  const whole = Number.isInteger(Math.round(Number(n) * 100) / 100);
  return '₹' + Number(n).toLocaleString('en-IN', { minimumFractionDigits: whole ? 0 : 2, maximumFractionDigits: whole ? 0 : 2 });
};
export const kg = (n?: number | null) => (n == null ? '—' : `${Number(n).toLocaleString('en-IN')} kg`);
export const date = (d?: string | null) =>
  d ? new Date(d.length === 10 ? d + 'T00:00:00' : d).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—';
export const dateTime = (d?: string | null) =>
  d ? new Date(d.endsWith('Z') || d.includes('+') ? d : d + 'Z').toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : '—';
export const today = () => new Date(Date.now() + 5.5 * 3600e3).toISOString().slice(0, 10);

// Friendly words and colours for every status code the API sends.
const STATUS: Record<string, [string, string]> = {
  QuotePending: ['Awaiting quote', 'amber'], Quoted: ['Awaiting payment', 'amber'], Confirmed: ['Finding a truck', 'orange'],
  Assigned: ['Truck assigned', 'blue'], InTransit: ['In transit', 'blue'], Delivered: ['Delivered', 'green'], Completed: ['Completed', 'green'],
  Cancelled: ['Cancelled', 'grey'], EnRouteToPickup: ['Going to pickup', 'blue'], AtPickup: ['At pickup', 'orange'], Loaded: ['Loaded', 'blue'],
  AtDestination: ['At destination', 'orange'], Pending: ['Under review', 'amber'], Approved: ['Verified', 'green'], Rejected: ['Rejected', 'red'],
  Verified: ['Verified', 'green'], Expired: ['Expired', 'red'], Available: ['Available', 'green'], Busy: ['Busy', 'orange'], Maintenance: ['Maintenance', 'red'],
  OnTrip: ['On a trip', 'blue'], OffDuty: ['Off duty', 'grey'], AwaitingPod: ['Waiting for POD', 'amber'], Released: ['Paid', 'green'],
  Captured: ['Paid', 'green'], Refunded: ['Refunded', 'grey'], Failed: ['Failed', 'red'], Active: ['Active', 'green'], Blocked: ['Blocked', 'red'],
  PendingKyc: ['KYC pending', 'amber'], Sent: ['Valid', 'green'], Accepted: ['Accepted', 'green'], Superseded: ['Replaced', 'grey']
};
export const statusLabel = (s: string) => STATUS[s]?.[0] ?? s;
export function Pill({ status, label }: { status: string; label?: string }) {
  const [text, color] = STATUS[status] ?? [status, 'grey'];
  return <span className={`pill ${color}`}>{label ?? text}</span>;
}

// ---------------- data loading ----------------
/** Loads data on mount and whenever `deps` change; `reload()` fetches again. */
export function useLoad<T>(fn: () => Promise<T>, deps: unknown[] = []) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const reload = useCallback(async () => {
    setLoading(true);
    try { setData(await fn()); setError(null); }
    catch (e) { setError(e instanceof ApiError ? e.message : 'Something went wrong.'); }
    finally { setLoading(false); }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
  useEffect(() => { reload(); }, [reload]);
  return { data, error, loading, reload, setData };
}

/** Wraps an async action with a busy flag and error message. */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const run = useCallback(async <T,>(fn: () => Promise<T>): Promise<T | undefined> => {
    setBusy(true); setError(null);
    try { return await fn(); }
    catch (e) { setError(e instanceof ApiError ? e.message : 'Something went wrong.'); return undefined; }
    finally { setBusy(false); }
  }, []);
  return { busy, error, setError, run };
}

// ---------------- building blocks ----------------
type BtnProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'dark' | 'secondary' | 'ghost' | 'danger' | 'success';
  size?: 'sm' | 'lg';
  block?: boolean;
  busy?: boolean;
};
export function Button({ variant = 'primary', size, block, busy, children, className, disabled, type = 'button', ...rest }: BtnProps) {
  const cls = ['btn', `btn-${variant}`, size && `btn-${size}`, block && 'btn-block', className].filter(Boolean).join(' ');
  return (
    <button type={type} className={cls} disabled={disabled || busy} {...rest}>
      {busy && <span className="spin" aria-hidden="true" />}
      {children}
    </button>
  );
}

export function Field({ label, hint, error, children, className }: { label: string; hint?: ReactNode; error?: string | null; children: ReactNode; className?: string }) {
  return (
    <label className={`field ${className ?? ''}`}>
      <span>{label}</span>
      {children}
      {error ? <em className="err">{error}</em> : hint ? <em className="hint">{hint}</em> : null}
    </label>
  );
}

export function PageHead({ title, sub, children }: { title: string; sub?: ReactNode; children?: ReactNode }) {
  return (
    <header className="page-head">
      <div>
        <h1>{title}</h1>
        {sub && <p>{sub}</p>}
      </div>
      {children && <div className="row">{children}</div>}
    </header>
  );
}

export function Alert({ kind = 'info', title, children }: { kind?: 'info' | 'warn' | 'error' | 'success'; title?: string; children?: ReactNode }) {
  return (
    <div className={`alert ${kind}`} role={kind === 'error' ? 'alert' : undefined}>
      <div>{title && <b>{title}</b>}{children}</div>
    </div>
  );
}

export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return <div className="empty"><b>{title}</b>{children}</div>;
}

export function Stat({ label, value, note, alert }: { label: string; value: ReactNode; note?: ReactNode; alert?: boolean }) {
  return <div className={`card stat ${alert ? 'alert' : ''}`}><small>{label}</small><b>{value}</b>{note && <span>{note}</span>}</div>;
}

export function Loading({ state }: { state: { loading: boolean; error: string | null; data: unknown } }) {
  if (state.error) return <Alert kind="error" title="Couldn't load this page">{state.error}</Alert>;
  if (state.loading && !state.data) return <div className="empty"><span className="spin" /> Loading…</div>;
  return null;
}

export function Route({ from, to }: { from?: string | null; to?: string | null }) {
  return <span className="route">{from ?? '—'} <span className="arrow" role="img" aria-label="to">→</span> {to ?? '—'}</span>;
}

export function Dialog({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  useEffect(() => {
    const k = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', k);
    return () => window.removeEventListener('keydown', k);
  }, [onClose]);
  return (
    <div className="dialog-back" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="dialog" role="dialog" aria-modal="true" aria-label={title}>
        <div className="row" style={{ justifyContent: 'space-between' }}>
          <h2>{title}</h2>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close">✕</Button>
        </div>
        {children}
      </div>
    </div>
  );
}

/** A reason prompt for reject actions (the API requires one). */
export function ReasonDialog({ title, onSubmit, onClose, busy }: { title: string; onSubmit: (reason: string) => void; onClose: () => void; busy?: boolean }) {
  const [reason, setReason] = useState('');
  return (
    <Dialog title={title} onClose={onClose}>
      <Field label="Reason shown to the applicant" hint="Say what to fix, for example “Insurance copy is unreadable”.">
        <textarea className="input" value={reason} onChange={(e) => setReason(e.target.value)} autoFocus />
      </Field>
      <div className="row end">
        <Button variant="secondary" onClick={onClose}>Cancel</Button>
        <Button variant="danger" busy={busy} disabled={!reason.trim()} onClick={() => onSubmit(reason.trim())}>Reject</Button>
      </div>
    </Dialog>
  );
}
