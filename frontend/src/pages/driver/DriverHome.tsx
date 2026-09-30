import { useEffect, useState } from 'react';
import { api } from '../../api';
import { useToast } from '../../state';
import { Alert, Button, Empty, Field, Loading, PageHead, Pill, Route, date, dateTime, kg, useAction, useLoad } from '../../components/ui';

interface TripRow { id: number; tripNumber: string; status: string; from: string; to: string; pickupDate: string; vehicle: string }
interface TripDetail {
  id: number; tripNumber: string; status: string; vehicle: string; hasPod: boolean; plannedDistanceKm?: number;
  booking: { bookingNumber: string; goodsDescription: string; weightKg: number; pickupDate: string; pickupSlot?: string; specialInstructions?: string;
    pickup: { city?: string; address: string; contact?: string; phone?: string }; drop: { city?: string; address: string; contact?: string; phone?: string } };
  events: { eventType: string; createdAt: string }[];
}

const STEPS = ['Go to pickup', 'Reached pickup', 'Pickup code', 'Start trip', 'Reached destination', 'Upload POD', 'Delivery code'];
function stepIndex(t: TripDetail) {
  switch (t.status) {
    case 'Assigned': return 0;
    case 'EnRouteToPickup': return 1;
    case 'AtPickup': return 2;
    case 'Loaded': return 3;
    case 'InTransit': return 4;
    case 'AtDestination': return t.hasPod ? 6 : 5;
    default: return 7;
  }
}

/** Best-effort location for the trip log; the trip still moves on if the phone refuses. */
function where(): Promise<{ latitude?: number; longitude?: number }> {
  return new Promise((resolve) => {
    if (!navigator.geolocation) return resolve({});
    navigator.geolocation.getCurrentPosition(
      (p) => resolve({ latitude: +p.coords.latitude.toFixed(6), longitude: +p.coords.longitude.toFixed(6) }),
      () => resolve({}), { timeout: 3000, maximumAge: 60000 });
  });
}
const maps = (a: string, city?: string) => `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(`${a}, ${city ?? ''}`)}`;

export function DriverHomePage() {
  const list = useLoad(() => api<TripRow[]>('/driver/trips'));
  const active = list.data?.find((t) => !['Delivered', 'Completed', 'Cancelled'].includes(t.status));
  const [openId, setOpenId] = useState<number | null>(null);
  const tripId = openId ?? active?.id ?? list.data?.[0]?.id ?? null;   // falls back to the latest trip so a just-finished delivery stays on screen

  return (
    <>
      <PageHead title="My trips" sub="One big button for the next step. Your owner and the customer see each step as you tap it." />
      <Loading state={list} />
      {list.data?.length === 0 && <div className="card"><Empty title="No trips assigned yet">When your owner takes a load for you, it appears here.</Empty></div>}
      <div className="grid split">
        <div className="driver-wrap">{tripId && <TripRunner id={tripId} onChanged={list.reload} />}</div>
        {!!list.data?.length && (
          <div className="card">
            <h2 style={{ marginBottom: 12 }}>All trips</h2>
            <div className="stack" style={{ gap: 8 }}>
              {list.data.map((t) => (
                <button key={t.id} type="button" onClick={() => setOpenId(t.id)} className="card" style={{ padding: 12, textAlign: 'left', cursor: 'pointer', borderColor: t.id === tripId ? 'var(--accent)' : undefined }}>
                  <div className="row" style={{ justifyContent: 'space-between' }}><Route from={t.from} to={t.to} /><Pill status={t.status} /></div>
                  <div className="muted small">{t.tripNumber} · {date(t.pickupDate)} · {t.vehicle}</div>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </>
  );
}

function TripRunner({ id, onChanged }: { id: number; onChanged: () => void }) {
  const toast = useToast();
  const s = useLoad(() => api<TripDetail>(`/driver/trips/${id}`), [id]);
  const act = useAction();
  const [code, setCode] = useState('');
  const [file, setFile] = useState<File | null>(null);
  useEffect(() => { setCode(''); setFile(null); }, [id, s.data?.status]);
  const t = s.data;
  if (!t) return <Loading state={s} />;
  const step = stepIndex(t);
  const b = t.booking;
  const done = () => { s.reload(); onChanged(); };

  const advance = (action: string, msg: string) => act.run(async () => {
    const loc = await where();
    await api(`/driver/trips/${id}/advance`, { body: { action, ...loc } });
    toast(msg); done();
  });
  const verify = (kind: 'Pickup' | 'Delivery') => act.run(async () => {
    await api(`/driver/trips/${id}/verify-otp`, { body: { kind, code } });
    toast(kind === 'Pickup' ? 'Code correct. Goods loaded.' : 'Delivered. Well done!'); done();
  });
  const upload = (kind: 'POD' | 'PickupPhoto') => act.run(async () => {
    const form = new FormData();
    form.set('kind', kind); form.set('file', file!);
    const loc = await where();
    if (loc.latitude) { form.set('latitude', String(loc.latitude)); form.set('longitude', String(loc.longitude)); }
    await api(`/driver/trips/${id}/photo`, { form });
    toast(kind === 'POD' ? 'Delivery receipt uploaded.' : 'Photo saved.'); setFile(null); done();
  });

  return (
    <div className="stack">
      <div className="card" style={{ background: 'var(--navy)', color: '#fff', borderColor: 'var(--navy)' }}>
        <div className="row" style={{ justifyContent: 'space-between' }}><span className="mono" style={{ color: '#9fb0d4' }}>{t.tripNumber} · {t.vehicle}</span><Pill status={t.status} /></div>
        <h2 style={{ marginTop: 8, color: '#fff' }}>{b.pickup.city} → {b.drop.city}</h2>
        <p style={{ color: '#b9c6e2' }}>{b.goodsDescription} · {kg(b.weightKg)}{t.plannedDistanceKm ? ` · ${t.plannedDistanceKm} km` : ''}</p>
      </div>

      {act.error && <Alert kind="error">{act.error}</Alert>}

      {step === 0 && <>
        <a className="big-btn navy" href={maps(b.pickup.address, b.pickup.city)} target="_blank" rel="noreferrer" style={{ textDecoration: 'none' }}>Open map to pickup</a>
        <button className="big-btn" disabled={act.busy} onClick={() => advance('EnRoute', 'Customer told you are on the way.')}>I'm on the way</button>
      </>}
      {step === 1 && <button className="big-btn" disabled={act.busy} onClick={() => advance('ReachedPickup', 'Marked as reached pickup.')}>I reached pickup</button>}
      {step === 2 && (
        <div className="card stack">
          <Field label="Pickup code from the sender" hint="The sender sees this 4-digit code in their booking.">
            <input className="input otp-input" inputMode="numeric" maxLength={4} value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))} autoFocus />
          </Field>
          <button className="big-btn" disabled={act.busy || code.length !== 4} onClick={() => verify('Pickup')}>Confirm goods loaded</button>
          <Field label="Photo of loaded goods (optional)"><input className="input" type="file" accept="image/*" capture="environment" onChange={(e) => setFile(e.target.files?.[0] ?? null)} /></Field>
          {file && <Button variant="secondary" busy={act.busy} onClick={() => upload('PickupPhoto')}>Save photo</Button>}
        </div>
      )}
      {step === 3 && <button className="big-btn green" disabled={act.busy} onClick={() => advance('StartTrip', 'Trip started. Drive safe!')}>Start trip</button>}
      {step === 4 && <>
        <a className="big-btn navy" href={maps(b.drop.address, b.drop.city)} target="_blank" rel="noreferrer" style={{ textDecoration: 'none' }}>Open map to delivery</a>
        <button className="big-btn" disabled={act.busy} onClick={() => advance('ReachedDestination', 'Marked as reached destination.')}>I reached the destination</button>
      </>}
      {step === 5 && (
        <div className="card stack">
          <Field label="Photo of the signed delivery receipt (POD)"><input className="input" type="file" accept="image/*,.pdf" capture="environment" onChange={(e) => setFile(e.target.files?.[0] ?? null)} /></Field>
          <button className="big-btn" disabled={act.busy || !file} onClick={() => upload('POD')}>Upload POD</button>
        </div>
      )}
      {step === 6 && (
        <div className="card stack">
          <Field label="Delivery code from the receiver" hint="The customer shares it with the receiver.">
            <input className="input otp-input" inputMode="numeric" maxLength={4} value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))} autoFocus />
          </Field>
          <button className="big-btn green" disabled={act.busy || code.length !== 4} onClick={() => verify('Delivery')}>Complete delivery</button>
        </div>
      )}
      {step === 7 && <Alert kind="success" title={t.status === 'Cancelled' ? 'Trip cancelled' : 'Trip delivered'}>{t.status === 'Cancelled' ? 'The customer cancelled this trip.' : 'Your owner is paid after Olekar checks the delivery receipt.'}</Alert>}

      <div className="card">
        <ul className="checklist" aria-label="Trip steps">
          {STEPS.map((label, i) => (
            <li key={label} className={i < step ? 'done' : i === step ? 'now' : ''}><span className="d">{i < step ? '✓' : ''}</span>{label}</li>
          ))}
        </ul>
      </div>
      <div className="card">
        <div className="kv">
          <div><small>Pickup</small><b>{b.pickup.address}</b><div className="muted small">{b.pickup.city}{b.pickup.contact ? ` · ${b.pickup.contact}` : ''} {b.pickup.phone && <span className="mono">{b.pickup.phone}</span>}</div></div>
          <div><small>Delivery</small><b>{b.drop.address}</b><div className="muted small">{b.drop.city}{b.drop.contact ? ` · ${b.drop.contact}` : ''} {b.drop.phone && <span className="mono">{b.drop.phone}</span>}</div></div>
          <div><small>Pickup date</small><b>{date(b.pickupDate)}</b><div className="muted small">{b.pickupSlot}</div></div>
          <div><small>Booking</small><b className="mono">{b.bookingNumber}</b></div>
          {b.specialInstructions && <div style={{ gridColumn: '1 / -1' }}><small>Instructions</small><b>{b.specialInstructions}</b></div>}
        </div>
        {t.events.length > 0 && <p className="muted small" style={{ marginTop: 12 }}>Last update {dateTime(t.events[t.events.length - 1].createdAt)}</p>}
      </div>
    </div>
  );
}
