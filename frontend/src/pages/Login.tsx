import { useState, type FormEvent, type ReactNode } from 'react';
import { api, type SessionUser } from '../api';
import { Link, navigate } from '../router';
import { useAuth } from '../state';
import { Alert, Button, Field, useAction } from '../components/ui';

export const homeFor = (role: SessionUser['role']) =>
  ({ Customer: '/customer/bookings', Owner: '/owner', Driver: '/driver', Admin: '/admin' })[role];

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="auth">
      <aside className="auth-art">
        <div className="row"><span className="brand-mark" aria-hidden="true">O</span><b style={{ fontFamily: 'var(--display)', fontSize: '1.1rem' }}>Olekar Logistics</b></div>
        <div className="stack">
          <h1>Move anything. <em>Anywhere.</em></h1>
          <p>Book verified lorries, take loads for your trucks, or run today's trip. One sign-in for customers, lorry owners, drivers and the Olekar team.</p>
        </div>
        <div className="road" aria-hidden="true" />
      </aside>
      <div className="auth-form"><div className="box">{children}</div></div>
    </div>
  );
}

/** Mobile number + "Send code" + code entry, shared by sign-in and registration. */
export function OtpStep({ purpose, mobile, setMobile, code, setCode }: {
  purpose: 'Login' | 'Signup'; mobile: string; setMobile: (v: string) => void; code: string; setCode: (v: string) => void;
}) {
  const [sent, setSent] = useState(false);
  const [devCode, setDevCode] = useState<string | null>(null);
  const send = useAction();
  const sendCode = () => send.run(async () => {
    const r = await api<{ sent: boolean; devCode?: string | null }>('/auth/otp/send', { body: { mobile, purpose } });
    setSent(true);
    setDevCode(r.devCode ?? null);
  });
  return (
    <div className="stack">
      <Field label="Mobile number" hint="10-digit Indian mobile number" error={send.error}>
        <div className="row" style={{ flexWrap: 'nowrap' }}>
          <input className="input" inputMode="numeric" autoComplete="tel" value={mobile} onChange={(e) => setMobile(e.target.value)} placeholder="98xxxxxxxx" aria-invalid={!!send.error} />
          <Button variant="secondary" busy={send.busy} disabled={mobile.replace(/\D/g, '').length < 10} onClick={sendCode}>{sent ? 'Resend' : 'Send code'}</Button>
        </div>
      </Field>
      {sent && (
        <Field label="6-digit code" hint={devCode ? <>Test mode: your code is <b className="mono">{devCode}</b></> : 'Sent by SMS. It is valid for 10 minutes.'}>
          <input className="input otp-input" inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))} autoFocus />
        </Field>
      )}
    </div>
  );
}

export function LoginPage() {
  const { signIn } = useAuth();
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const login = useAction();
  const submit = (e: FormEvent) => {
    e.preventDefault();
    login.run(async () => {
      const r = await api<{ token: string; user: SessionUser }>('/auth/login', { body: { mobile, code } });
      signIn(r.token, r.user);
      navigate(homeFor(r.user.role), true);
    });
  };
  return (
    <AuthLayout>
      <form className="stack" onSubmit={submit} style={{ gap: 20 }}>
        <div className="stack" style={{ gap: 6 }}><h1>Sign in</h1><p className="muted">We'll send a one-time code to your mobile.</p></div>
        <OtpStep purpose="Login" mobile={mobile} setMobile={setMobile} code={code} setCode={setCode} />
        {login.error && <Alert kind="error">{login.error}</Alert>}
        <Button type="submit" size="lg" block busy={login.busy} disabled={code.length !== 6}>Sign in</Button>
        <p className="muted small">New to Olekar? <Link to="/register">Create an account</Link></p>
      </form>
    </AuthLayout>
  );
}
