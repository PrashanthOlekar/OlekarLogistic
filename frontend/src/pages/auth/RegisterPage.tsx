import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { useAuth } from '../../auth';
import { Alert, Button, Field } from '../../components';
import { OtpStep } from '../../features/auth/OtpStep';
import {
  ACCOUNT_KINDS,
  FIRST_PAGE,
  register as registerAccount,
  type AccountKind,
  type RegistrationForm,
} from '../../features/auth/registration';
import { useAction } from '../../hooks/useAction';
import { AuthLayout } from '../../layouts/AuthLayout';
import { today } from '../../utils/format';

/** Returns an onChange handler that stores the input's value under `key`. */
type Bind = (key: string) => {
  value: string;
  onChange: (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => void;
};

export function RegisterPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [kind, setKind] = useState<AccountKind>('customer');
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const [form, setForm] = useState<RegistrationForm>({ licenceClass: 'TRANSPORT' });
  const register = useAction();

  const bind: Bind = (key) => ({
    value: form[key] ?? '',
    onChange: (event) => setForm((current) => ({ ...current, [key]: event.target.value })),
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    register.run(async () => {
      const result = await registerAccount(kind, form, mobile, code);
      signIn(result);
      navigate(FIRST_PAGE[kind], { replace: true });
    });
  };

  const detailsTitle = { customer: 'Your details', owner: 'Owner details', driver: 'Driver details' }[kind];
  const documentsNeeded =
    kind === 'owner' ? 'Aadhaar, PAN and cancelled cheque' : 'licence, Aadhaar and photo';

  return (
    <AuthLayout>
      <form className="stack loose" onSubmit={submit}>
        <div className="stack tight">
          <h1>Create your account</h1>
          <p className="muted">It takes about 3 minutes.</p>
        </div>

        <AccountKindPicker value={kind} onChange={setKind} />

        <div className="form-section">
          <h3>
            <span className="n">1</span>Verify your mobile
          </h3>
          <OtpStep
            purpose="Signup"
            mobile={mobile}
            onMobileChange={setMobile}
            code={code}
            onCodeChange={setCode}
          />
        </div>

        <div className="form-section">
          <h3>
            <span className="n">2</span>
            {detailsTitle}
          </h3>
          <Field label="Full name">
            <input className="input" autoComplete="name" {...bind('fullName')} />
          </Field>
          {kind === 'customer' && <CustomerFields bind={bind} />}
          {kind === 'owner' && <OwnerFields bind={bind} />}
          {kind === 'driver' && <DriverFields bind={bind} />}
        </div>

        {kind === 'owner' && (
          <div className="form-section">
            <h3>
              <span className="n">3</span>Bank account for payouts
            </h3>
            <BankFields bind={bind} />
          </div>
        )}

        {register.error && <Alert kind="error">{register.error}</Alert>}

        {kind !== 'customer' && (
          <Alert kind="info">
            Next you'll upload your {documentsNeeded}. Our team verifies them, usually within a day.
          </Alert>
        )}

        <Button type="submit" size="lg" block busy={register.busy} disabled={code.length !== 6}>
          Create account
        </Button>

        <p className="muted small">
          Already registered? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </AuthLayout>
  );
}

function AccountKindPicker({
  value,
  onChange,
}: {
  value: AccountKind;
  onChange: (kind: AccountKind) => void;
}) {
  return (
    <div className="grid g3 tight" role="radiogroup" aria-label="Account type">
      {ACCOUNT_KINDS.map((option) => (
        <button
          key={option.id}
          type="button"
          role="radio"
          className="choice"
          aria-checked={value === option.id}
          onClick={() => onChange(option.id)}
        >
          <b>{option.label}</b>
          <span className="muted small">{option.sub}</span>
        </button>
      ))}
    </div>
  );
}

function CustomerFields({ bind }: { bind: Bind }) {
  return (
    <>
      <div className="grid g2">
        <Field label="Company (optional)">
          <input className="input" {...bind('companyName')} />
        </Field>
        <Field label="GSTIN (optional)" hint="For GST invoices">
          <input className="input" maxLength={15} {...bind('gstin')} />
        </Field>
      </div>
      <Field label="Email (optional)">
        <input className="input" type="email" autoComplete="email" {...bind('email')} />
      </Field>
    </>
  );
}

function OwnerFields({ bind }: { bind: Bind }) {
  return (
    <div className="grid g2">
      <Field label="Business name (optional)">
        <input className="input" {...bind('businessName')} />
      </Field>
      <Field label="Email (optional)">
        <input className="input" type="email" {...bind('email')} />
      </Field>
      <Field label="PAN" hint="Stored encrypted">
        <input className="input" maxLength={10} placeholder="ABCDE1234F" {...bind('pan')} />
      </Field>
      <Field label="Aadhaar, last 4 digits" hint="We never store the full number">
        <input className="input" inputMode="numeric" maxLength={4} {...bind('aadhaarLast4')} />
      </Field>
    </div>
  );
}

function DriverFields({ bind }: { bind: Bind }) {
  return (
    <>
      <div className="grid g2">
        <Field label="Driving licence number">
          <input className="input" placeholder="KA01 20110045678" {...bind('licenceNumber')} />
        </Field>
        <Field label="Licence class">
          <select className="input" {...bind('licenceClass')}>
            <option value="LMV">LMV (up to 7.5 t)</option>
            <option value="TRANSPORT">Transport</option>
            <option value="HGMV">HGMV</option>
            <option value="HPMV">HPMV</option>
          </select>
        </Field>
        <Field label="Licence valid until">
          <input className="input" type="date" min={today()} {...bind('licenceExpiry')} />
        </Field>
        <Field label="Aadhaar, last 4 digits (optional)">
          <input className="input" inputMode="numeric" maxLength={4} {...bind('aadhaarLast4')} />
        </Field>
        <Field label="Emergency contact name">
          <input className="input" {...bind('emergencyContactName')} />
        </Field>
        <Field label="Emergency contact phone">
          <input className="input" inputMode="tel" {...bind('emergencyContactPhone')} />
        </Field>
      </div>
      <Field
        label="Your lorry owner's mobile"
        hint="Links you to the owner whose trucks you drive. Leave empty if you own your truck."
      >
        <input className="input" inputMode="tel" {...bind('ownerMobile')} />
      </Field>
    </>
  );
}

function BankFields({ bind }: { bind: Bind }) {
  return (
    <div className="grid g2">
      <Field label="Account holder name">
        <input className="input" {...bind('accountHolder')} />
      </Field>
      <Field label="Bank name (optional)">
        <input className="input" {...bind('bankName')} />
      </Field>
      <Field label="Account number" hint="Stored encrypted; only the last 4 digits are shown">
        <input className="input" inputMode="numeric" {...bind('accountNumber')} />
      </Field>
      <Field label="IFSC">
        <input className="input" maxLength={11} placeholder="HDFC0001234" {...bind('ifsc')} />
      </Field>
    </div>
  );
}
