import { useState } from 'react';
import { api, openDocument } from '../../api';
import { Link } from '../../router';
import { useToast } from '../../state';
import { Alert, Button, Dialog, Empty, Field, Loading, PageHead, Pill, ReasonDialog, Route, Stat, date, dateTime, inr, kg, useAction, useLoad } from '../../components/ui';
import { docName } from '../owner/OwnerPages';

// ---------------------------------------------------------------- dashboard
interface Summary {
  bookingsToday: number; activeTrips: number; completedThisMonth: number; awaitingPayment: number; awaitingTruck: number;
  revenueThisMonth: number; commissionThisMonth: number; pendingApprovals: number; pendingDocuments: number; podToApprove: number; settlementsToRelease: number;
  last7: { date: string; count: number }[]; routes: { route: string; count: number }[]; cities: { city: string; count: number }[]; vehicles: { status: string; count: number }[];
}

export function AdminHomePage() {
  const s = useLoad(() => api<Summary>('/admin/summary'));
  const d = s.data;
  const max7 = Math.max(1, ...(d?.last7.map((x) => x.count) ?? [1]));
  const bars = (rows: { label: string; count: number }[]) => {
    const m = Math.max(1, ...rows.map((r) => r.count));
    return rows.length ? <div className="bars">{rows.map((r) => <div className="bar" key={r.label}><span>{r.label}</span><span className="track"><i style={{ width: `${(r.count / m) * 100}%` }} /></span><b className="tnum" style={{ textAlign: 'right' }}>{r.count}</b></div>)}</div> : <Empty title="No data yet" />;
  };
  return (
    <>
      <PageHead title="Operations dashboard" sub="Live from the database. Times are in IST." ><Button variant="secondary" size="sm" onClick={s.reload}>Refresh</Button></PageHead>
      <Loading state={s} />
      {d && <>
        <div className="grid g4" style={{ marginBottom: 16 }}>
          <Stat label="Bookings today" value={d.bookingsToday} />
          <Stat label="Active trips" value={d.activeTrips} />
          <Stat label="Completed this month" value={d.completedThisMonth} />
          <Stat label="Paid, waiting for a truck" value={d.awaitingTruck} alert={d.awaitingTruck > 0} />
          <Stat label="Collected this month" value={inr(d.revenueThisMonth)} note="Customer payments" />
          <Stat label="Commission this month" value={inr(d.commissionThisMonth)} />
          <Stat label="Unpaid quotes" value={d.awaitingPayment} />
          <Stat label="Approved vehicles" value={d.vehicles.reduce((a, v) => a + v.count, 0)} note={d.vehicles.map((v) => `${v.count} ${v.status.toLowerCase()}`).join(' · ') || 'None yet'} />
        </div>

        <div className="card" style={{ marginBottom: 16 }}>
          <h2 style={{ marginBottom: 12 }}>Needs action</h2>
          <div className="grid g4">
            <Link to="/admin/approvals" className="card stat" style={{ textDecoration: 'none', color: 'inherit' }}><small>Approvals</small><b>{d.pendingApprovals}</b><span>Owners, drivers, vehicles</span></Link>
            <Link to="/admin/documents" className="card stat" style={{ textDecoration: 'none', color: 'inherit' }}><small>Documents</small><b>{d.pendingDocuments}</b><span>Waiting for review</span></Link>
            <Link to="/admin/trips" className="card stat" style={{ textDecoration: 'none', color: 'inherit' }}><small>POD to approve</small><b>{d.podToApprove}</b><span>Delivered trips</span></Link>
            <Link to="/admin/settlements" className="card stat" style={{ textDecoration: 'none', color: 'inherit' }}><small>Payouts to send</small><b>{d.settlementsToRelease}</b><span>Owner settlements</span></Link>
          </div>
        </div>

        <div className="grid g3">
          <div className="card">
            <h2 style={{ marginBottom: 16 }}>Bookings, last 7 days</h2>
            <div className="cols7">
              {d.last7.map((x) => (
                <div key={x.date}><b>{x.count}</b><i style={{ height: `${(x.count / max7) * 110 + 3}px` }} /><span>{new Date(x.date + 'T00:00:00').toLocaleDateString('en-IN', { weekday: 'short' })}</span></div>
              ))}
            </div>
          </div>
          <div className="card"><h2 style={{ marginBottom: 12 }}>Top routes</h2>{bars(d.routes.map((r) => ({ label: r.route, count: r.count })))}</div>
          <div className="card"><h2 style={{ marginBottom: 12 }}>Bookings by pickup city</h2>{bars(d.cities.map((r) => ({ label: r.city, count: r.count })))}</div>
        </div>
      </>}
    </>
  );
}

// ---------------------------------------------------------------- approvals
type DocLite = { id: number; docType: string; status: string; expiryDate?: string };
interface Approvals {
  owners: { id: number; name: string; mobile: string; businessName?: string; panLast4?: string; aadhaarLast4?: string; bank?: string; createdAt: string; documents: DocLite[] }[];
  drivers: { id: number; name: string; mobile: string; licenceNumber: string; licenceClass: string; licenceExpiry: string; owner?: string; createdAt: string; documents: DocLite[] }[];
  vehicles: { id: number; registrationNumber: string; vehicleType: string; capacityKg: number; owner: string; createdAt: string; documents: DocLite[] }[];
}

function Docs({ docs }: { docs: DocLite[] }) {
  const toast = useToast();
  if (!docs.length) return <span className="small" style={{ color: 'var(--amber)' }}>No documents yet</span>;
  return (
    <div className="row" style={{ gap: 6 }}>
      {docs.map((d) => (
        <button key={d.id} type="button" className={`pill ${d.status === 'Verified' ? 'green' : d.status === 'Rejected' ? 'red' : 'grey'}`} style={{ border: 0, cursor: 'pointer' }}
          onClick={() => openDocument(d.id).catch((e) => toast(e.message, 'error'))}>{docName(d.docType)}</button>
      ))}
    </div>
  );
}

export function ApprovalsPage() {
  const toast = useToast();
  const s = useLoad(() => api<Approvals>('/admin/approvals'));
  const [tab, setTab] = useState<'owners' | 'drivers' | 'vehicles'>('owners');
  const [rejecting, setRejecting] = useState<{ kind: string; id: number; name: string } | null>(null);
  const act = useAction();
  const review = (kind: string, id: number, approve: boolean, reason?: string) => act.run(async () => {
    await api(`/admin/approvals/${kind}/${id}`, { body: { approve, reason } });
    toast(approve ? 'Approved.' : 'Rejected and the applicant has been told why.'); setRejecting(null); s.reload();
  });
  const d = s.data;
  const actions = (kind: string, id: number, name: string) => (
    <div className="row" style={{ gap: 6 }}>
      <Button size="sm" variant="success" busy={act.busy} onClick={() => review(kind, id, true)}>Approve</Button>
      <Button size="sm" variant="danger" onClick={() => setRejecting({ kind, id, name })}>Reject</Button>
    </div>
  );
  return (
    <>
      <PageHead title="Approvals" sub="Open each document before approving. Approved owners, drivers and vehicles can start taking trips." />
      {act.error && <div style={{ marginBottom: 12 }}><Alert kind="error">{act.error}</Alert></div>}
      <div className="tabs" role="tablist">
        {(['owners', 'drivers', 'vehicles'] as const).map((t) => (
          <button key={t} role="tab" aria-selected={tab === t} onClick={() => setTab(t)}>{t[0].toUpperCase() + t.slice(1)} {d ? `(${d[t].length})` : ''}</button>
        ))}
      </div>
      <Loading state={s} />
      {d && tab === 'owners' && (d.owners.length ? <div className="card table-wrap"><table>
        <thead><tr><th>Owner</th><th>KYC</th><th>Bank</th><th>Documents</th><th>Applied</th><th /></tr></thead>
        <tbody>{d.owners.map((o) => <tr key={o.id}><td><b>{o.name}</b><div className="muted small">{o.businessName} <span className="mono">{o.mobile}</span></div></td><td className="small">PAN ••{o.panLast4}<br />Aadhaar ••{o.aadhaarLast4}</td><td className="mono small">{o.bank}</td><td><Docs docs={o.documents} /></td><td className="small">{date(o.createdAt)}</td><td>{actions('owner', o.id, o.name)}</td></tr>)}</tbody>
      </table></div> : <div className="card"><Empty title="No owners waiting" /></div>)}
      {d && tab === 'drivers' && (d.drivers.length ? <div className="card table-wrap"><table>
        <thead><tr><th>Driver</th><th>Licence</th><th>Owner</th><th>Documents</th><th /></tr></thead>
        <tbody>{d.drivers.map((x) => <tr key={x.id}><td><b>{x.name}</b><div className="mono small muted">{x.mobile}</div></td><td className="small"><span className="mono">{x.licenceNumber}</span><br />{x.licenceClass} · until {date(x.licenceExpiry)}</td><td>{x.owner ?? <span className="muted">Own vehicle</span>}</td><td><Docs docs={x.documents} /></td><td>{actions('driver', x.id, x.name)}</td></tr>)}</tbody>
      </table></div> : <div className="card"><Empty title="No drivers waiting" /></div>)}
      {d && tab === 'vehicles' && (d.vehicles.length ? <div className="card table-wrap"><table>
        <thead><tr><th>Vehicle</th><th>Owner</th><th>Documents</th><th /></tr></thead>
        <tbody>{d.vehicles.map((v) => <tr key={v.id}><td><b className="mono">{v.registrationNumber}</b><div className="muted small">{v.vehicleType} · {kg(v.capacityKg)}</div></td><td>{v.owner}</td><td><Docs docs={v.documents} /></td><td>{actions('vehicle', v.id, v.registrationNumber)}</td></tr>)}</tbody>
      </table></div> : <div className="card"><Empty title="No vehicles waiting" /></div>)}
      {rejecting && <ReasonDialog title={`Reject ${rejecting.name}`} busy={act.busy} onClose={() => setRejecting(null)} onSubmit={(r) => review(rejecting.kind, rejecting.id, false, r)} />}
    </>
  );
}

// ---------------------------------------------------------------- documents
interface DocRow { id: number; entityType: string; entityId: number; docType: string; fileName: string; documentNumber?: string; expiryDate?: string; status: string; rejectionReason?: string; uploadedAt: string }
export function AdminDocumentsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('Pending');
  const s = useLoad(() => api<DocRow[]>(`/admin/documents${status ? `?status=${status}` : ''}`), [status]);
  const [rejecting, setRejecting] = useState<DocRow | null>(null);
  const act = useAction();
  const review = (id: number, approve: boolean, reason?: string) => act.run(async () => {
    await api(`/admin/documents/${id}/review`, { body: { approve, reason } }); setRejecting(null); toast(approve ? 'Document verified.' : 'Document rejected.'); s.reload();
  });
  return (
    <>
      <PageHead title="Documents" sub="KYC and vehicle documents. Check the expiry date against the file.">
        <div className="seg">{['Pending', 'Verified', 'Rejected', ''].map((x) => <button key={x || 'all'} aria-pressed={status === x} onClick={() => setStatus(x)}>{x || 'All'}</button>)}</div>
      </PageHead>
      {act.error && <div style={{ marginBottom: 12 }}><Alert kind="error">{act.error}</Alert></div>}
      <Loading state={s} />
      {s.data?.length === 0 && <div className="card"><Empty title="Nothing here" /></div>}
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Document</th><th>Belongs to</th><th>Number</th><th>Valid until</th><th>Uploaded</th><th>Status</th><th /></tr></thead>
        <tbody>{s.data.map((d) => (
          <tr key={d.id}>
            <td><button type="button" className="btn btn-ghost btn-sm" onClick={() => openDocument(d.id).catch((e) => toast(e.message, 'error'))}>{docName(d.docType)} ↗</button></td>
            <td className="small">{d.entityType} #{d.entityId}</td><td className="mono small">{d.documentNumber ?? '—'}</td><td>{date(d.expiryDate)}</td><td className="small">{dateTime(d.uploadedAt)}</td>
            <td><Pill status={d.status} /></td>
            <td>{d.status === 'Pending' && <div className="row" style={{ gap: 6 }}><Button size="sm" variant="success" busy={act.busy} onClick={() => review(d.id, true)}>Verify</Button><Button size="sm" variant="danger" onClick={() => setRejecting(d)}>Reject</Button></div>}</td>
          </tr>
        ))}</tbody>
      </table></div>}
      {rejecting && <ReasonDialog title={`Reject ${docName(rejecting.docType)}`} busy={act.busy} onClose={() => setRejecting(null)} onSubmit={(r) => review(rejecting.id, false, r)} />}
    </>
  );
}

// ---------------------------------------------------------------- bookings
interface AdminBooking { id: number; bookingNumber: string; status: string; customer: string; customerMobile: string; from: string; to: string; pickupDate: string; weightKg: number; vehicleType: string; total?: number; createdAt: string }
interface Assignable { id: number; registrationNumber: string; owner: string; currentDriverId?: number; drivers: { id: number; name: string }[] }
export function AdminBookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('');
  const s = useLoad(() => api<AdminBooking[]>(`/admin/bookings${status ? `?status=${status}` : ''}`), [status]);
  const [assigning, setAssigning] = useState<AdminBooking | null>(null);
  const [options, setOptions] = useState<Assignable[] | null>(null);
  const [vehicleId, setVehicleId] = useState(0);
  const [driverId, setDriverId] = useState(0);
  const act = useAction();
  const openAssign = (b: AdminBooking) => act.run(async () => {
    setAssigning(b); setOptions(null);
    const o = await api<Assignable[]>(`/admin/bookings/${b.id}/assignable`);
    setOptions(o); setVehicleId(o[0]?.id ?? 0); setDriverId(o[0]?.drivers[0]?.id ?? 0);
  });
  const assign = () => act.run(async () => {
    const r = await api<{ tripNumber: string }>(`/admin/bookings/${assigning!.id}/assign`, { body: { vehicleId, driverId } });
    toast(`Assigned. Trip ${r.tripNumber} created.`); setAssigning(null); s.reload();
  });
  const chosen = options?.find((o) => o.id === vehicleId);
  return (
    <>
      <PageHead title="Bookings" sub="Paid bookings are offered to owners automatically. Assign one yourself if no owner takes it.">
        <div className="seg">{[['', 'All'], ['Quoted', 'Unpaid'], ['Confirmed', 'Needs truck'], ['Assigned', 'Assigned'], ['InTransit', 'In transit'], ['Delivered', 'Delivered']].map(([v, l]) => <button key={v || 'all'} aria-pressed={status === v} onClick={() => setStatus(v)}>{l}</button>)}</div>
      </PageHead>
      <Loading state={s} />
      {s.data?.length === 0 && <div className="card"><Empty title="No bookings" /></div>}
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Booking</th><th>Customer</th><th>Route</th><th>Pickup</th><th>Load</th><th className="num">Total</th><th>Status</th><th /></tr></thead>
        <tbody>{s.data.map((b) => (
          <tr key={b.id}>
            <td className="mono">{b.bookingNumber}</td><td><b>{b.customer}</b><div className="mono small muted">{b.customerMobile}</div></td>
            <td><Route from={b.from} to={b.to} /></td><td>{date(b.pickupDate)}</td><td className="small">{b.vehicleType}<br />{kg(b.weightKg)}</td>
            <td className="num">{inr(b.total)}</td><td><Pill status={b.status} /></td>
            <td>{b.status === 'Confirmed' && <Button size="sm" variant="dark" onClick={() => openAssign(b)}>Assign truck</Button>}</td>
          </tr>
        ))}</tbody>
      </table></div>}
      {assigning && (
        <Dialog title={`Assign ${assigning.bookingNumber}`} onClose={() => setAssigning(null)}>
          <p className="muted">{assigning.from} → {assigning.to} · {assigning.vehicleType} · {kg(assigning.weightKg)}</p>
          {!options && <Loading state={{ loading: true, error: null, data: null }} />}
          {options?.length === 0 && <Alert kind="warn" title="No verified, free vehicle of this type">Approve more vehicles or ask owners to mark trucks Available.</Alert>}
          {!!options?.length && <>
            <Field label="Vehicle"><select className="input" value={vehicleId} onChange={(e) => { const v = Number(e.target.value); setVehicleId(v); setDriverId(options.find((o) => o.id === v)?.drivers[0]?.id ?? 0); }}>{options.map((o) => <option key={o.id} value={o.id}>{o.registrationNumber} · {o.owner}</option>)}</select></Field>
            <Field label="Driver" error={chosen && !chosen.drivers.length ? "This owner has no verified driver free right now." : null}><select className="input" value={driverId} onChange={(e) => setDriverId(Number(e.target.value))}>{chosen?.drivers.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}</select></Field>
          </>}
          {act.error && <Alert kind="error">{act.error}</Alert>}
          <Button block busy={act.busy} disabled={!vehicleId || !driverId} onClick={assign}>Assign</Button>
        </Dialog>
      )}
    </>
  );
}

// ---------------------------------------------------------------- trips & POD
interface AdminTrip { id: number; tripNumber: string; status: string; bookingNumber: string; from: string; to: string; vehicle: string; driver: string; driverMobile: string; owner: string; createdAt: string; deliveredAt?: string; pod?: number | null; lastEvent?: string }
export function AdminTripsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('');
  const s = useLoad(() => api<AdminTrip[]>(`/admin/trips${status ? `?status=${status}` : ''}`), [status]);
  const act = useAction();
  const approve = (t: AdminTrip) => act.run(async () => { await api(`/admin/trips/${t.id}/approve-pod`, { method: 'POST' }); toast(`POD approved. Invoice issued and ${t.owner}'s payout is ready to send.`); s.reload(); });
  return (
    <>
      <PageHead title="Trips & POD" sub="Open the delivery receipt and approve it to close the trip, issue the invoice and unlock the owner's payout.">
        <div className="seg">{[['', 'All'], ['InTransit', 'In transit'], ['Delivered', 'POD to approve'], ['Completed', 'Completed']].map(([v, l]) => <button key={v || 'all'} aria-pressed={status === v} onClick={() => setStatus(v)}>{l}</button>)}</div>
      </PageHead>
      {act.error && <div style={{ marginBottom: 12 }}><Alert kind="error">{act.error}</Alert></div>}
      <Loading state={s} />
      {s.data?.length === 0 && <div className="card"><Empty title="No trips" /></div>}
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Trip</th><th>Route</th><th>Vehicle</th><th>Driver</th><th>Owner</th><th>Status</th><th /></tr></thead>
        <tbody>{s.data.map((t) => (
          <tr key={t.id}>
            <td className="mono">{t.tripNumber}<div className="small muted">{t.bookingNumber}</div></td><td><Route from={t.from} to={t.to} /></td>
            <td className="mono">{t.vehicle}</td><td>{t.driver}<div className="mono small muted">{t.driverMobile}</div></td><td>{t.owner}</td>
            <td><Pill status={t.status} />{t.deliveredAt && <div className="small muted">{dateTime(t.deliveredAt)}</div>}</td>
            <td>{t.status === 'Delivered' && <div className="row" style={{ gap: 6 }}>
              {t.pod && <Button size="sm" variant="secondary" onClick={() => openDocument(t.pod!).catch((e) => toast(e.message, 'error'))}>View POD</Button>}
              <Button size="sm" variant="success" busy={act.busy} onClick={() => approve(t)}>Approve POD</Button>
            </div>}</td>
          </tr>
        ))}</tbody>
      </table></div>}
    </>
  );
}

// ---------------------------------------------------------------- money
interface PaymentRow { id: number; bookingNumber: string; customer: string; amount: number; method: string; gateway: string; status: string; paidAt?: string; gatewayPaymentId?: string }
export function PaymentsPage() {
  const s = useLoad(() => api<PaymentRow[]>('/admin/payments'));
  return (
    <>
      <PageHead title="Payments" sub="Customer payments. Money is held until delivery is confirmed." />
      <Loading state={s} />
      {s.data?.length === 0 && <div className="card"><Empty title="No payments yet" /></div>}
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Paid</th><th>Booking</th><th>Customer</th><th>Method</th><th>Reference</th><th className="num">Amount</th><th>Status</th></tr></thead>
        <tbody>{s.data.map((p) => <tr key={p.id}><td>{dateTime(p.paidAt)}</td><td className="mono">{p.bookingNumber}</td><td>{p.customer}</td><td>{p.method}{p.gateway === 'Test' && <span className="muted small"> · test</span>}</td><td className="mono small">{p.gatewayPaymentId}</td><td className="num">{inr(p.amount)}</td><td><Pill status={p.status} /></td></tr>)}</tbody>
      </table></div>}
    </>
  );
}

interface SettlementRow { id: number; trip: string; owner: string; bank?: string; grossAmount: number; commissionAmount: number; tdsAmount: number; netAmount: number; status: string; utr?: string; releasedAt?: string }
export function SettlementsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('Approved');
  const s = useLoad(() => api<SettlementRow[]>(`/admin/settlements${status ? `?status=${status}` : ''}`), [status]);
  const [releasing, setReleasing] = useState<SettlementRow | null>(null);
  const [utr, setUtr] = useState('');
  const act = useAction();
  const release = () => act.run(async () => {
    await api(`/admin/settlements/${releasing!.id}/release`, { body: { utr } });
    toast(`${inr(releasing!.netAmount)} recorded as paid to ${releasing!.owner}.`); setReleasing(null); setUtr(''); s.reload();
  });
  return (
    <>
      <PageHead title="Owner payouts" sub="Send the amount from your bank or gateway payout dashboard, then record the UTR here.">
        <div className="seg">{[['Approved', 'Ready to pay'], ['AwaitingPod', 'Waiting for POD'], ['Released', 'Paid'], ['', 'All']].map(([v, l]) => <button key={v || 'all'} aria-pressed={status === v} onClick={() => setStatus(v)}>{l}</button>)}</div>
      </PageHead>
      <Loading state={s} />
      {s.data?.length === 0 && <div className="card"><Empty title="Nothing here" /></div>}
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Trip</th><th>Owner</th><th>Bank</th><th className="num">Freight</th><th className="num">Commission</th><th className="num">Pay owner</th><th>Status</th><th /></tr></thead>
        <tbody>{s.data.map((x) => (
          <tr key={x.id}><td className="mono">{x.trip}</td><td>{x.owner}</td><td className="mono small">{x.bank ?? '—'}</td><td className="num">{inr(x.grossAmount)}</td><td className="num">{inr(x.commissionAmount)}</td><td className="num"><b>{inr(x.netAmount)}</b></td>
            <td><Pill status={x.status} />{x.utr && <div className="mono small muted">{x.utr}</div>}</td>
            <td>{x.status === 'Approved' && <Button size="sm" variant="success" onClick={() => setReleasing(x)}>Mark paid</Button>}</td></tr>
        ))}</tbody>
      </table></div>}
      {releasing && (
        <Dialog title={`Pay ${releasing.owner}`} onClose={() => setReleasing(null)}>
          <div className="money-rows">
            <div><span>Freight</span><span>{inr(releasing.grossAmount)}</span></div>
            <div><span>Olekar commission</span><span>− {inr(releasing.commissionAmount)}</span></div>
            <div className="total"><span>Send to {releasing.bank}</span><b>{inr(releasing.netAmount)}</b></div>
          </div>
          <Field label="Bank transfer reference (UTR)"><input className="input mono" value={utr} onChange={(e) => setUtr(e.target.value.toUpperCase())} autoFocus /></Field>
          {act.error && <Alert kind="error">{act.error}</Alert>}
          <Button block busy={act.busy} disabled={!utr.trim()} onClick={release}>Record payout</Button>
        </Dialog>
      )}
    </>
  );
}

// ---------------------------------------------------------------- users
interface UserRow { id: number; role: string; fullName: string; mobile: string; email?: string; status: string; createdAt: string; lastLoginAt?: string }
export function UsersPage() {
  const toast = useToast();
  const [role, setRole] = useState('');
  const [q, setQ] = useState('');
  const [query, setQuery] = useState('');
  const s = useLoad(() => api<UserRow[]>(`/admin/users?${new URLSearchParams({ ...(role && { role }), ...(query && { q: query }) })}`), [role, query]);
  const act = useAction();
  const toggle = (u: UserRow) => act.run(async () => {
    await api(`/admin/users/${u.id}/block`, { body: { block: u.status !== 'Blocked' } });
    toast(u.status === 'Blocked' ? `${u.fullName} unblocked.` : `${u.fullName} blocked. They are signed out now.`); s.reload();
  });
  return (
    <>
      <PageHead title="Users" sub="Block accounts that show fraud or abuse. Blocking takes effect immediately.">
        <form className="row" onSubmit={(e) => { e.preventDefault(); setQuery(q); }}>
          <input className="input" style={{ width: 220 }} placeholder="Name or mobile" value={q} onChange={(e) => setQ(e.target.value)} aria-label="Search users" />
          <select className="input" style={{ width: 150 }} value={role} onChange={(e) => setRole(e.target.value)} aria-label="Role"><option value="">All roles</option><option>Customer</option><option>Owner</option><option>Driver</option><option>Admin</option></select>
        </form>
      </PageHead>
      {act.error && <div style={{ marginBottom: 12 }}><Alert kind="error">{act.error}</Alert></div>}
      <Loading state={s} />
      {!!s.data?.length && <div className="card table-wrap"><table>
        <thead><tr><th>Name</th><th>Role</th><th>Mobile</th><th>Joined</th><th>Last sign-in</th><th>Status</th><th /></tr></thead>
        <tbody>{s.data.map((u) => (
          <tr key={u.id}><td><b>{u.fullName}</b>{u.email && <div className="small muted">{u.email}</div>}</td><td>{u.role}</td><td className="mono">{u.mobile}</td><td>{date(u.createdAt)}</td><td>{dateTime(u.lastLoginAt)}</td><td><Pill status={u.status} /></td>
            <td>{u.role !== 'Admin' && <Button size="sm" variant={u.status === 'Blocked' ? 'secondary' : 'danger'} busy={act.busy} onClick={() => toggle(u)}>{u.status === 'Blocked' ? 'Unblock' : 'Block'}</Button>}</td></tr>
        ))}</tbody>
      </table></div>}
      {s.data?.length === 0 && <div className="card"><Empty title="No users match" /></div>}
    </>
  );
}
