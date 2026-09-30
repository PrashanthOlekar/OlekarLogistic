import { useState, type FormEvent } from 'react';
import { api, getMeta, openDocument, type Meta } from '../../api';
import { useToast } from '../../state';
import { Link } from '../../router';
import { Alert, Button, Dialog, Empty, Field, Loading, PageHead, Pill, Route, Stat, date, dateTime, inr, kg, useAction, useLoad } from '../../components/ui';

interface Summary { kycStatus: string; rejectionReason?: string; vehicles: number; vehiclesApproved: number; drivers: number; activeTrips: number; earnedThisMonth: number; pendingPayout: number; bank?: { accountHolder: string; accountLast4: string; ifsc: string } }
interface Load { id: number; bookingNumber: string; pickupDate: string; pickupSlot?: string; weightKg: number; goods: string; from: string; fromAddress: string; to: string; vehicleType: string; distanceKm?: number; payout?: number; vehicles: { id: number; registrationNumber: string; currentDriverId?: number | null }[] }
interface DriverRow { id: number; name: string; mobile: string; licenceNumber: string; licenceClass: string; licenceExpiry: string; kycStatus: string; dutyStatus: string; rating?: number | null }

export function OwnerHomePage() {
  const toast = useToast();
  const sum = useLoad(() => api<Summary>('/owner/summary'));
  const loads = useLoad(() => api<{ kycApproved: boolean; loads: Load[] }>('/owner/loads'));
  const drivers = useLoad(() => api<DriverRow[]>('/owner/drivers'));
  const [taking, setTaking] = useState<Load | null>(null);
  const [vehicleId, setVehicleId] = useState(0);
  const [driverId, setDriverId] = useState(0);
  const act = useAction();

  const open = (l: Load) => {
    setTaking(l);
    const v = l.vehicles[0];
    setVehicleId(v?.id ?? 0);
    const free = (drivers.data ?? []).filter((d) => d.kycStatus === 'Approved' && d.dutyStatus === 'Available');
    setDriverId(v?.currentDriverId && free.some((d) => d.id === v.currentDriverId) ? v.currentDriverId : free[0]?.id ?? 0);
  };
  const accept = () => act.run(async () => {
    const r = await api<{ tripNumber: string }>(`/owner/loads/${taking!.id}/accept`, { body: { vehicleId, driverId } });
    toast(`Load taken. Trip ${r.tripNumber} is assigned to your driver.`);
    setTaking(null); loads.reload(); sum.reload(); drivers.reload();
  });
  const decline = (l: Load) => act.run(async () => { await api(`/owner/loads/${l.id}/decline`, { method: 'POST' }); loads.reload(); });

  const s = sum.data;
  const freeDrivers = (drivers.data ?? []).filter((d) => d.kycStatus === 'Approved' && d.dutyStatus === 'Available');

  return (
    <>
      <PageHead title="Loads & overview" sub="Paid loads that match your verified, available trucks appear here." />
      <Loading state={sum} />
      {s && s.kycStatus !== 'Approved' && (
        <div style={{ marginBottom: 16 }}>
          <Alert kind={s.kycStatus === 'Rejected' ? 'error' : 'warn'} title={s.kycStatus === 'Rejected' ? 'Your KYC was not approved' : 'Your KYC is under review'}>
            {s.kycStatus === 'Rejected' ? <>Reason: {s.rejectionReason}. Upload corrected documents in <Link to="/owner/documents">KYC documents</Link>.</> : <>Upload your Aadhaar, PAN and cancelled cheque in <Link to="/owner/documents">KYC documents</Link>. You can add vehicles and drivers meanwhile.</>}
          </Alert>
        </div>
      )}
      {s && (
        <div className="grid g4" style={{ marginBottom: 20 }}>
          <Stat label="Earned this month" value={inr(s.earnedThisMonth)} note="Paid to your bank" />
          <Stat label="Pending payout" value={inr(s.pendingPayout)} note="After delivery proof is approved" />
          <Stat label="Vehicles" value={`${s.vehiclesApproved}/${s.vehicles}`} note="Verified / total" />
          <Stat label="Active trips" value={s.activeTrips} note={`${s.drivers} driver${s.drivers === 1 ? '' : 's'} linked`} />
        </div>
      )}

      <div className="card">
        <div className="card-head"><h2>Available loads</h2><Button variant="ghost" size="sm" onClick={() => loads.reload()}>Refresh</Button></div>
        <Loading state={loads} />
        {loads.data && !loads.data.kycApproved && <Empty title="Loads appear once your KYC is approved" />}
        {loads.data?.kycApproved && loads.data.loads.length === 0 && (
          <Empty title="No matching loads right now">Loads show up when a customer pays for a trip that fits one of your verified, available trucks. {s && s.vehiclesApproved === 0 && <><br /><Link to="/owner/vehicles">Add a vehicle and its documents</Link> to get verified.</>}</Empty>
        )}
        <div className="stack">
          {loads.data?.loads.map((l) => (
            <div key={l.id} className="card" style={{ padding: 16 }}>
              <div className="row" style={{ justifyContent: 'space-between' }}>
                <Route from={l.from} to={l.to} />
                <b style={{ font: '800 1.4rem var(--display)' }}>{inr(l.payout)}</b>
              </div>
              <p className="muted small" style={{ margin: '6px 0 12px' }}>
                {l.goods} · {kg(l.weightKg)} · {l.vehicleType} · {l.distanceKm} km · pickup {date(l.pickupDate)}{l.pickupSlot ? `, ${l.pickupSlot}` : ''} · <span className="mono">{l.bookingNumber}</span>
              </p>
              <div className="row">
                <Button onClick={() => open(l)}>Take this load</Button>
                <Button variant="ghost" onClick={() => decline(l)}>Not interested</Button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {taking && (
        <Dialog title={`Take ${taking.from} → ${taking.to}`} onClose={() => setTaking(null)}>
          <p className="muted">You'll receive <b style={{ color: 'var(--ink)' }}>{inr(taking.payout)}</b> after delivery is confirmed (freight minus Olekar's commission).</p>
          <Field label="Vehicle">
            <select className="input" value={vehicleId} onChange={(e) => setVehicleId(Number(e.target.value))}>
              {taking.vehicles.map((v) => <option key={v.id} value={v.id}>{v.registrationNumber}</option>)}
            </select>
          </Field>
          <Field label="Driver" error={freeDrivers.length === 0 ? 'No verified driver is free. Ask a driver to register with your mobile number.' : null}>
            <select className="input" value={driverId} onChange={(e) => setDriverId(Number(e.target.value))}>
              {freeDrivers.map((d) => <option key={d.id} value={d.id}>{d.name} · {d.mobile}</option>)}
            </select>
          </Field>
          {act.error && <Alert kind="error">{act.error}</Alert>}
          <Button size="lg" block busy={act.busy} disabled={!vehicleId || !driverId} onClick={accept}>Confirm and assign</Button>
        </Dialog>
      )}
    </>
  );
}

// ---------------------------------------------------------------- vehicles
interface VehicleRow { id: number; registrationNumber: string; vehicleType: string; vehicleTypeId: number; capacityKg: number; makeModel?: string; availabilityStatus: string; verificationStatus: string; currentDriverId?: number | null; driverName?: string | null; documents: { id: number; docType: string; status: string; expiryDate?: string }[] }
const VEHICLE_DOCS = ['RC', 'Insurance', 'Fitness', 'Permit', 'PUC'];
const DOC_NAMES: Record<string, string> = { RC: 'RC (registration)', Insurance: 'Insurance', Fitness: 'Fitness certificate', Permit: 'Permit', PUC: 'Pollution (PUC)', Aadhaar: 'Aadhaar', PAN: 'PAN card', CancelledCheque: 'Cancelled cheque', GST: 'GST certificate', Licence: 'Driving licence', DriverPhoto: 'Photo', Other: 'Other' };
export const docName = (t: string) => DOC_NAMES[t] ?? t;

export function VehiclesPage() {
  const toast = useToast();
  const list = useLoad(() => api<VehicleRow[]>('/owner/vehicles'));
  const drivers = useLoad(() => api<DriverRow[]>('/owner/drivers'));
  const [meta, setMeta] = useState<Meta | null>(null);
  const [adding, setAdding] = useState(false);
  const [uploadFor, setUploadFor] = useState<VehicleRow | null>(null);
  const [f, setF] = useState({ registrationNumber: '', vehicleTypeId: 0, capacityKg: 0, makeModel: '', manufactureYear: '' });
  const act = useAction();

  const startAdd = async () => {
    const m = meta ?? await getMeta(); setMeta(m);
    setF((s) => ({ ...s, vehicleTypeId: s.vehicleTypeId || m.vehicleTypes[3]?.id || m.vehicleTypes[0].id, capacityKg: s.capacityKg || (m.vehicleTypes[3]?.maxLoadKg ?? 0) }));
    setAdding(true);
  };
  const add = (e: FormEvent) => {
    e.preventDefault();
    act.run(async () => {
      await api('/owner/vehicles', { body: { ...f, manufactureYear: f.manufactureYear ? Number(f.manufactureYear) : null, homeCityId: null } });
      toast('Vehicle added. Upload its documents to get it verified.'); setAdding(false);
      setF({ registrationNumber: '', vehicleTypeId: 0, capacityKg: 0, makeModel: '', manufactureYear: '' }); list.reload();
    });
  };
  const setAvail = (v: VehicleRow, status: string) => act.run(async () => { await api(`/owner/vehicles/${v.id}/availability`, { method: 'PATCH', body: { status } }); list.reload(); });
  const setDriver = (v: VehicleRow, driverId: number) => act.run(async () => { await api(`/owner/vehicles/${v.id}/driver`, { method: 'PATCH', body: { driverId: driverId || null } }); list.reload(); });

  return (
    <>
      <PageHead title="Vehicles" sub="Each vehicle needs its RC, insurance, fitness, permit and PUC verified before it gets loads.">
        <Button onClick={startAdd}>Add vehicle</Button>
      </PageHead>
      {act.error && !adding && <div style={{ marginBottom: 12 }}><Alert kind="error">{act.error}</Alert></div>}
      <Loading state={list} />
      {list.data?.length === 0 && <div className="card"><Empty title="No vehicles yet">Add your first lorry to start receiving loads.</Empty></div>}
      <div className="grid g2">
        {list.data?.map((v) => {
          const missing = VEHICLE_DOCS.filter((d) => !v.documents.some((x) => x.docType === d && x.status !== 'Rejected'));
          return (
            <div key={v.id} className="card stack">
              <div className="row" style={{ justifyContent: 'space-between' }}>
                <div><b className="mono" style={{ fontSize: '1.1rem' }}>{v.registrationNumber}</b><div className="muted small">{v.vehicleType} · {kg(v.capacityKg)}{v.makeModel ? ` · ${v.makeModel}` : ''}</div></div>
                <Pill status={v.verificationStatus} />
              </div>
              <div className="seg" role="group" aria-label={`Availability of ${v.registrationNumber}`}>
                {['Available', 'Busy', 'Maintenance'].map((st) => <button key={st} type="button" aria-pressed={v.availabilityStatus === st} onClick={() => setAvail(v, st)}>{st}</button>)}
              </div>
              <Field label="Regular driver">
                <select className="input" value={v.currentDriverId ?? 0} onChange={(e) => setDriver(v, Number(e.target.value))}>
                  <option value={0}>Not set</option>
                  {drivers.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </Field>
              <div>
                <small className="muted">Documents</small>
                <div className="row" style={{ marginTop: 6, gap: 6 }}>
                  {v.documents.map((d) => <button key={d.id} type="button" className="pill grey" style={{ border: 0, cursor: 'pointer' }} onClick={() => openDocument(d.id)}>{docName(d.docType)}: {d.status === 'Verified' ? '✓' : d.status === 'Rejected' ? '✕' : '…'}</button>)}
                </div>
                {missing.length > 0 && <p className="small" style={{ color: 'var(--amber)', marginTop: 6 }}>Missing: {missing.map(docName).join(', ')}</p>}
              </div>
              <Button variant="secondary" size="sm" onClick={() => setUploadFor(v)}>Upload document</Button>
            </div>
          );
        })}
      </div>

      {adding && meta && (
        <Dialog title="Add a vehicle" onClose={() => setAdding(false)}>
          <form className="stack" onSubmit={add}>
            <Field label="Registration number" hint="As printed on the RC"><input className="input" value={f.registrationNumber} onChange={(e) => setF({ ...f, registrationNumber: e.target.value.toUpperCase() })} placeholder="KA 01 AB 4521" autoFocus /></Field>
            <Field label="Vehicle type">
              <select className="input" value={f.vehicleTypeId} onChange={(e) => { const t = meta.vehicleTypes.find((x) => x.id === Number(e.target.value)); setF({ ...f, vehicleTypeId: Number(e.target.value), capacityKg: t?.maxLoadKg ?? f.capacityKg }); }}>
                {meta.vehicleTypes.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </Field>
            <div className="grid g2">
              <Field label="Load capacity (kg)"><input className="input" type="number" value={f.capacityKg} onChange={(e) => setF({ ...f, capacityKg: Number(e.target.value) })} /></Field>
              <Field label="Year (optional)"><input className="input" inputMode="numeric" maxLength={4} value={f.manufactureYear} onChange={(e) => setF({ ...f, manufactureYear: e.target.value })} /></Field>
            </div>
            <Field label="Make and model (optional)"><input className="input" value={f.makeModel} onChange={(e) => setF({ ...f, makeModel: e.target.value })} placeholder="Tata 1109g LPT" /></Field>
            {act.error && <Alert kind="error">{act.error}</Alert>}
            <Button type="submit" block busy={act.busy}>Add vehicle</Button>
          </form>
        </Dialog>
      )}
      {uploadFor && (
        <UploadDialog title={`Upload for ${uploadFor.registrationNumber}`} entityType="Vehicle" entityId={uploadFor.id} docTypes={[...VEHICLE_DOCS, 'Other']}
          onClose={() => setUploadFor(null)} onDone={() => { setUploadFor(null); toast('Document uploaded for review.'); list.reload(); }} />
      )}
    </>
  );
}

/** Shared upload form for KYC and vehicle documents. */
export function UploadDialog({ title, entityType, entityId, docTypes, onClose, onDone }: { title: string; entityType: string; entityId?: number; docTypes: string[]; onClose: () => void; onDone: () => void }) {
  const [docType, setDocType] = useState(docTypes[0]);
  const [number, setNumber] = useState('');
  const [expiry, setExpiry] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const act = useAction();
  const needsExpiry = ['Insurance', 'Fitness', 'Permit', 'PUC', 'Licence'].includes(docType);
  const submit = () => act.run(async () => {
    const form = new FormData();
    form.set('entityType', entityType);
    if (entityId) form.set('entityId', String(entityId));
    form.set('docType', docType);
    if (number && docType !== 'Aadhaar') form.set('documentNumber', number);
    if (expiry) form.set('expiryDate', expiry);
    form.set('file', file!);
    await api('/documents', { form });
    onDone();
  });
  return (
    <Dialog title={title} onClose={onClose}>
      <Field label="Document"><select className="input" value={docType} onChange={(e) => setDocType(e.target.value)}>{docTypes.map((d) => <option key={d} value={d}>{docName(d)}</option>)}</select></Field>
      {docType !== 'Aadhaar' && docType !== 'DriverPhoto' && <Field label="Document number (optional)"><input className="input" value={number} onChange={(e) => setNumber(e.target.value)} /></Field>}
      {docType === 'Aadhaar' && <Alert kind="info">Mask the first 8 digits of your Aadhaar before uploading. We only need the last 4.</Alert>}
      {needsExpiry && <Field label="Valid until"><input className="input" type="date" value={expiry} onChange={(e) => setExpiry(e.target.value)} /></Field>}
      <Field label="File" hint="PDF or photo, up to 10 MB"><input className="input" type="file" accept=".pdf,image/*" onChange={(e) => setFile(e.target.files?.[0] ?? null)} /></Field>
      {act.error && <Alert kind="error">{act.error}</Alert>}
      <Button block busy={act.busy} disabled={!file || (needsExpiry && !expiry)} onClick={submit}>Upload</Button>
    </Dialog>
  );
}

// ---------------------------------------------------------------- drivers
export function DriversPage() {
  const list = useLoad(() => api<DriverRow[]>('/owner/drivers'));
  return (
    <>
      <PageHead title="Drivers" sub="Drivers link themselves to you by entering your mobile number when they register." />
      <Loading state={list} />
      {list.data?.length === 0 && <div className="card"><Empty title="No drivers linked yet">Ask your driver to open the Olekar portal, choose “I drive”, and enter your mobile number as their owner.</Empty></div>}
      {!!list.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Driver</th><th>Mobile</th><th>Licence</th><th>Valid until</th><th>KYC</th><th>Duty</th></tr></thead>
            <tbody>{list.data.map((d) => (
              <tr key={d.id}><td><b>{d.name}</b></td><td className="mono">{d.mobile}</td><td className="mono">{d.licenceNumber} <span className="muted">({d.licenceClass})</span></td><td>{date(d.licenceExpiry)}</td><td><Pill status={d.kycStatus} /></td><td><Pill status={d.dutyStatus} /></td></tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </>
  );
}

// ---------------------------------------------------------------- trips & payouts
interface OwnerTrip { id: number; tripNumber: string; status: string; bookingNumber: string; from: string; to: string; pickupDate: string; vehicle: string; driver: string; ownerPayout: number; deliveredAt?: string }
export function OwnerTripsPage() {
  const list = useLoad(() => api<OwnerTrip[]>('/owner/trips'));
  return (
    <>
      <PageHead title="Trips" sub="Trips your vehicles are running or have completed." />
      <Loading state={list} />
      {list.data?.length === 0 && <div className="card"><Empty title="No trips yet">Take a load from the overview page to start.</Empty></div>}
      {!!list.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Trip</th><th>Route</th><th>Pickup</th><th>Vehicle</th><th>Driver</th><th className="num">Your payout</th><th>Status</th></tr></thead>
            <tbody>{list.data.map((t) => (
              <tr key={t.id}><td className="mono">{t.tripNumber}</td><td><Route from={t.from} to={t.to} /></td><td>{date(t.pickupDate)}</td><td className="mono">{t.vehicle}</td><td>{t.driver}</td><td className="num">{inr(t.ownerPayout)}</td><td><Pill status={t.status} /></td></tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </>
  );
}

interface PayoutRow { id: number; trip: string; grossAmount: number; commissionAmount: number; tdsAmount: number; netAmount: number; status: string; utr?: string; releasedAt?: string; createdAt: string }
export function PayoutsPage() {
  const list = useLoad(() => api<PayoutRow[]>('/owner/settlements'));
  const sum = useLoad(() => api<Summary>('/owner/summary'));
  return (
    <>
      <PageHead title="Payouts" sub={sum.data?.bank ? <>Paid to {sum.data.bank.accountHolder}, account ending {sum.data.bank.accountLast4} ({sum.data.bank.ifsc}), after the delivery proof is approved.</> : 'Paid to your bank after the delivery proof is approved.'} />
      <Loading state={list} />
      {list.data?.length === 0 && <div className="card"><Empty title="No payouts yet">Your first payout appears here after your first delivery.</Empty></div>}
      {!!list.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Trip</th><th className="num">Freight</th><th className="num">Commission</th><th className="num">TDS</th><th className="num">You receive</th><th>Status</th><th>Bank reference</th></tr></thead>
            <tbody>{list.data.map((p) => (
              <tr key={p.id}><td className="mono">{p.trip}</td><td className="num">{inr(p.grossAmount)}</td><td className="num">− {inr(p.commissionAmount)}</td><td className="num">{p.tdsAmount ? `− ${inr(p.tdsAmount)}` : '—'}</td><td className="num"><b>{inr(p.netAmount)}</b></td><td><Pill status={p.status} /></td><td className="mono small">{p.utr ? `${p.utr} · ${dateTime(p.releasedAt)}` : '—'}</td></tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </>
  );
}
