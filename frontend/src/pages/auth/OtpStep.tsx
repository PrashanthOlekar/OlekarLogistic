/** Mobile number → "Send code" → 6-digit code. Shared by sign-in and registration. */
import { useState } from 'react';
import { Button, Field } from '../../components';
import { useAction } from '../../hooks/useAction';
import { api } from '../../lib/api';

interface OtpStepProps {
  purpose: 'Login' | 'Signup';
  mobile: string;
  onMobileChange: (value: string) => void;
  code: string;
  onCodeChange: (value: string) => void;
}

interface SendCodeResult {
  sent: boolean;
  /** Only returned by a development server, so you can test without SMS. */
  devCode?: string | null;
}

export function OtpStep({ purpose, mobile, onMobileChange, code, onCodeChange }: OtpStepProps) {
  const [sent, setSent] = useState(false);
  const [devCode, setDevCode] = useState<string | null>(null);
  const send = useAction();

  const sendCode = () =>
    send.run(async () => {
      const result = await api<SendCodeResult>('/auth/otp/send', { body: { mobile, purpose } });
      setSent(true);
      setDevCode(result.devCode ?? null);
    });

  const hasFullNumber = mobile.replace(/\D/g, '').length >= 10;

  const codeHint = devCode ? (
    <>
      Test mode: your code is <b className="mono">{devCode}</b>
    </>
  ) : (
    'Sent by SMS. It is valid for 10 minutes.'
  );

  return (
    <div className="stack">
      <Field label="Mobile number" hint="10-digit Indian mobile number" error={send.error}>
        <div className="row nowrap">
          <input
            className="input"
            inputMode="numeric"
            autoComplete="tel"
            placeholder="98xxxxxxxx"
            value={mobile}
            onChange={(event) => onMobileChange(event.target.value)}
            aria-invalid={!!send.error}
          />
          <Button variant="secondary" busy={send.busy} disabled={!hasFullNumber} onClick={sendCode}>
            {sent ? 'Resend' : 'Send code'}
          </Button>
        </div>
      </Field>

      {sent && (
        <Field label="6-digit code" hint={codeHint}>
          <input
            className="input otp-input"
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={6}
            value={code}
            onChange={(event) => onCodeChange(event.target.value.replace(/\D/g, ''))}
            autoFocus
          />
        </Field>
      )}
    </div>
  );
}
