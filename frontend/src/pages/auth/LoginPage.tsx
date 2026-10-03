import { useState, type FormEvent } from 'react';
import { Alert, Button } from '../../components';
import { useAction } from '../../hooks/useAction';
import { AuthLayout } from '../../layout/AuthLayout';
import { HOME_PAGES } from '../../layout/navigation';
import { api } from '../../lib/api';
import { Link, navigate } from '../../lib/router';
import type { SignInResult } from '../../lib/types';
import { useAuth } from '../../state/AuthContext';
import { OtpStep } from './OtpStep';

export function LoginPage() {
  const { signIn } = useAuth();
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const login = useAction();

  const submit = (event: FormEvent) => {
    event.preventDefault();
    login.run(async () => {
      const result = await api<SignInResult>('/auth/login', { body: { mobile, code } });
      signIn(result.token, result.user);
      navigate(HOME_PAGES[result.user.role], true);
    });
  };

  return (
    <AuthLayout>
      <form className="stack loose" onSubmit={submit}>
        <div className="stack tight">
          <h1>Sign in</h1>
          <p className="muted">We'll send a one-time code to your mobile.</p>
        </div>

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
