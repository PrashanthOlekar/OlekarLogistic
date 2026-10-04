import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router';
import { authApi } from '../../api/authApi';
import { useAuth } from '../../auth';
import type { SessionEndReason } from '../../auth/sessionEvents';
import { Alert, Button } from '../../components';
import { OtpStep } from '../../features/auth/OtpStep';
import { useAction } from '../../hooks/useAction';
import { AuthLayout } from '../../layouts/AuthLayout';
import { HOME_PAGES } from '../../routes/navigation';

const END_MESSAGES: Partial<Record<SessionEndReason, string>> = {
  expired: 'Your session has ended. Please sign in again.',
  'account-inactive': 'This account is not active. Contact ProCargo support.',
};

export function LoginPage() {
  const { signIn, endReason } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const login = useAction();

  const submit = (event: FormEvent) => {
    event.preventDefault();
    login.run(async () => {
      const result = await authApi.login({ mobile, code });
      signIn(result);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? HOME_PAGES[result.user.role], { replace: true });
    });
  };

  return (
    <AuthLayout>
      <form className="stack loose" onSubmit={submit}>
        <div className="stack tight">
          <h1>Sign in</h1>
          <p className="muted">We'll send a one-time code to your mobile.</p>
        </div>

        {endReason && END_MESSAGES[endReason] && <Alert kind="warn">{END_MESSAGES[endReason]}</Alert>}

        <OtpStep
          purpose="Login"
          mobile={mobile}
          onMobileChange={setMobile}
          code={code}
          onCodeChange={setCode}
        />

        {login.error && <Alert kind="error">{login.error}</Alert>}

        <Button type="submit" size="lg" block busy={login.busy} disabled={code.length !== 6}>
          Sign in
        </Button>

        <p className="muted small">
          New to ProCargo? <Link to="/register">Create an account</Link>
        </p>
      </form>
    </AuthLayout>
  );
}
