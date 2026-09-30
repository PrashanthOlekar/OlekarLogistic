import { useState, type FormEvent } from 'react';
import { api, type SessionUser } from '../api';
import { Link, navigate } from '../router';
import { useAuth } from '../state';
import { Alert, Button, Field, today, useAction } from '../components/ui';
import { AuthLayout, OtpStep } from './Login';

type Kind = 'customer' | 'owner' | 'driver';
const KINDS: { id: Kind; label: string; sub: string }[] = [
  { id: 'customer', label: 'I need transport', sub: 'Book lorries for your goods' },
  { id: 'owner', label: 'I own lorries', sub: 'Get loads for your vehicles' },
  { id: 'driver', label: 'I drive', sub: 'Run trips for an owner' }
];

export function RegisterPage() {
  const { signIn } = useAuth();
  const [kind, setKind] = useState<Kind>('customer');
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const [f, setF] = useState<Record<string, string>>({ licenceClass: 'TRANSPORT' });
  const set = (k: string) => (e: { target: { value: string } }) => setF((s) => ({ ...s, [k]: e.target.value }));
  const reg = useAction();

  const submit = (e: FormEvent) => {
    e.preventDefault();
    reg.run(async () => {
      const base = { fullName: f.fullName ?? '', mobile, code, email: f.email || null };
      const body =
        kind === 'customer' ? { ...base, companyName: f.companyName || null, gstin: f.gstin || null }
        : kind === 'owner' ? { ...base, businessName: f.businessName || null, pan: f.pan ?? '', aadhaarLast4: f.aadhaarLast4 ?? '', accountHolder: f.accountHolder ?? '', accountNumber: f.accountNumber ?? '', ifsc: f.ifsc ?? '', bankName: f.bankName || null }
        : { ...base, licenceNumber: f.licenceNumber ?? '', licenceClass: f.licenceClass, licenceExpiry: f.licenceExpiry ?? '', aadhaarLast4: f.aadhaarLast4 || null, emergencyContactName: f.emergencyContactName || null, emergencyContactPhone: f.emergencyContactPhone || null, ownerMobile: f.ownerMobile || null };
      const r = await api<{ token: string; user: SessionUser }>(`/auth/register/${kind}`, { body });
      signIn(r.token, r.user);
      navigate(kind === 'customer' ? '/customer/book' : kind === 'owner' ? '/owner/documents' : '/driver/documents', true);
    });
  };

  return (
    <AuthLayout>
      <form className="stack" onSubmit={submit} style={{ gap: 20 }}>
        <div className="stack" style={{ gap: 6 }}><h1>Create your account</h1><p className="muted">It takes about 3 minutes.</p></div>
        <div className="grid g3" role="radiogroup" aria-label="Account type" style={{ gap: 8 }}>
          {KINDS.map((k) => (
            <button key={k.id} type="button" role="radio" aria-checked={kind === k.id} onClick={() => setKind(k.id)}
              className="card" style={{ textAlign: 'left', cursor: 'pointer', padding: 12, borderColor: kind === k.id ? 'var(--accent)' : undefined, boxShadow: kind === k.id ? '0 0 0 3px rgba(255,122,26,.15)' : undefined }}>
              <b style={{ display: 'block', fontSize: '.9rem' }}>{k.label}</b><span className="muted small">{k.sub}</span>
            </button>
          ))}
        </div>

        <div className="form-section">
          <h3><span className="n">1</span>Verify your mobile</h3>
          <OtpStep purpose="Signup" mobile={mobile} setMobile={setMobile} code={code} setCode={setCode} />
        </div>

        <div className="form-section">
          <h3><span className="n">2</span>{kind === 'owner' ? 'Owner details' : kind === 'driver' ? 'Driver details' : 'Your details'}</h3>
          <Field label="Full name"><input className="input" autoComplete="name" value={f.fullName ?? ''} onChange={set('fullName')} /></Field>
          {kind === 'customer' && <>
            <div className="grid g2">
              <Field label="Company (optional)"><input className="input" value={f.companyName ?? ''} onChange={set('companyName')} /></Field>
              <Field label="GSTIN (optional)" hint="For GST invoices"><input className="input" value={f.gstin ?? ''} onChange={set('gstin')} maxLength={15} /></Field>
            </div>
            <Field label="Email (optional)"><input className="input" type="email" autoComplete="email" value={f.email ?? ''} onChange={set('email')} /></Field>
          </>}
          {kind === 'owner' && <>
            <div className="grid g2">
              <Field label="Business name (optional)"><input className="input" value={f.businessName ?? ''} onChange={set('businessName')} /></Field>
              <Field label="Email (optional)"><input className="input" type="email" value={f.email ?? ''} onChange={set('email')} /></Field>
              <Field label="PAN" hint="Stored encrypted"><input className="input" value={f.pan ?? ''} onChange={set('pan')} maxLength={10} placeholder="ABCDE1234F" /></Field>
              <Field label="Aadhaar, last 4 digits" hint="We never store the full number"><input className="input" inputMode="numeric" value={f.aadhaarLast4 ?? ''} onChange={set('aadhaarLast4')} maxLength={4} /></Field>
            </div>
          </>}
          {kind === 'driver' && <>
            <div className="grid g2">
              <Field label="Driving licence number"><input className="input" value={f.licenceNumber ?? ''} onChange={set('licenceNumber')} placeholder="KA01 20110045678" /></Field>
              <Field label="Licence class">
                <select className="input" value={f.licenceClass} onChange={set('licenceClass')}>
                  <option value="LMV">LMV (up to 7.5 t)</option><option value="TRANSPORT">Transport</option><option value="HGMV">HGMV</option><option value="HPMV">HPMV</option>
                </select>
              </Field>
              <Field label="Licence valid until"><input className="input" type="date" min={today()} value={f.licenceExpiry ?? ''} onChange={set('licenceExpiry')} /></Field>
              <Field label="Aadhaar, last 4 digits (optional)"><input className="input" inputMode="numeric" maxLength={4} value={f.aadhaarLast4 ?? ''} onChange={set('aadhaarLast4')} /></Field>
              <Field label="Emergency contact name"><input className="input" value={f.emergencyContactName ?? ''} onChange={set('emergencyContactName')} /></Field>
              <Field label="Emergency contact phone"><input className="input" inputMode="tel" value={f.emergencyContactPhone ?? ''} onChange={set('emergencyContactPhone')} /></Field>
            </div>
            <Field label="Your lorry owner's mobile" hint="Links you to the owner whose trucks you drive. Leave empty if you own your truck."><input className="input" inputMode="tel" value={f.ownerMobile ?? ''} onChange={set('ownerMobile')} /></Field>
          </>}
        </div>

        {kind === 'owner' && (
          <div className="form-section">
            <h3><span className="n">3</span>Bank account for payouts</h3>
            <div className="grid g2">
              <Field label="Account holder name"><input className="input" value={f.accountHolder ?? ''} onChange={set('accountHolder')} /></Field>
              <Field label="Bank name (optional)"><input className="input" value={f.bankName ?? ''} onChange={set('bankName')} /></Field>
              <Field label="Account number" hint="Stored encrypted; only the last 4 digits are shown"><input className="input" inputMode="numeric" value={f.accountNumber ?? ''} onChange={set('accountNumber')} /></Field>
              <Field label="IFSC"><input className="input" value={f.ifsc ?? ''} onChange={set('ifsc')} maxLength={11} placeholder="HDFC0001234" /></Field>
            </div>
          </div>
        )}

        {reg.error && <Alert kind="error">{reg.error}</Alert>}
        {kind !== 'customer' && <Alert kind="info">Next you'll upload your {kind === 'owner' ? 'Aadhaar, PAN and cancelled cheque' : 'licence, Aadhaar and photo'}. Our team verifies them, usually within a day.</Alert>}
        <Button type="submit" size="lg" block busy={reg.busy} disabled={code.length !== 6}>Create account</Button>
        <p className="muted small">Already registered? <Link to="/login">Sign in</Link></p>
      </form>
    </AuthLayout>
  );
}
