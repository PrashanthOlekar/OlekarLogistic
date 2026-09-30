import { useState } from 'react';
import { api } from '../../api';
import { Link, navigate } from '../../router';
import { useToast } from '../../state';
import { Alert, Button, Dialog, Empty, Field, Loading, PageHead, Pill, Route, date, dateTime, inr, kg, useAction, useLoad } from '../../components/ui';

interface BookingRow { id: number; bookingNumber: string; status: string; pickupDate: string; weightKg: number; from: string; to: string; vehicleType: string; total?: number | null }

export function BookingsPage() {
  const s = useLoad(() => api<BookingRow[]>('/bookings'));
  return (
    <>
      <PageHead title="My bookings" sub="Every trip you've booked, newest first.">
        <Link to="/customer/book" className="btn btn-primary">Book a truck</Link>
      </PageHead>
      <Loading state={s} />
      {s.data && (s.data.length === 0 ? (
        <div className="card"><Empty title="No bookings yet">Book your first truck and it will appear here.</Empty></div>
      ) : (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Booking</th><th>Route</th><th>Pickup</th><th>Vehicle</th><th className="num">Weight</th><th className="num">Total</th><th>Status</th></tr></thead>
            <tbody>
              {s.data.map((b) => (
                <tr key={b.id} className="link" onClick={() => navigate(`/customer/bookings/${b.id}`)}>
                  <td><Link to={`/customer/bookings/${b.id}`} className="mono">{b.bookingNumber}</Link></td>
                  <td><Route from={b.from} to={b.to} /></td>
                  <td>{date(b.pickupDate)}</td>
                  <td>{b.vehicleType}</td>
                  <td className="num">{kg(b.weightKg)}</td>
                  <td className="num">{inr(b.total)}</td>
                  <td><Pill status={b.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </>
  );
}

interface BookingDetail {
  id: number; bookingNumber: string; status: string; pickupDate: string; pickupSlot?: string; weightKg: number; goodsDescription: string; goods: string; vehicleType: string;
  specialInstructions?: string; cancelledReason?: string;
  pickup: { city?: string; address: string; contact?: string; phone?: string };
  drop: { city?: string; address: string; contact?: string; phone?: string };
  quote?: { distanceKm: number; vehicleCost: number; driverCost: number; taxAmount: number; totalAmount: number; validUntil: string; status: string; expired: boolean } | null;
  payment?: { method: string; gateway: string; status: string; amount: number; paidAt?: string; gatewayPaymentId?: string } | null;
  trip?: { tripNumber: string; status: string; vehicle: string; driverName: string; driverMobile: string; pickupOtp?: string | null; deliveryOtp?: string | null; events: { eventType: string; note?: string; createdAt: string }[] } | null;
  invoice?: { invoiceNumber: string; taxableAmount: number; cgst: number; sgst: number; igst: number; totalAmount: number; issuedAt: string } | null;
}

const FLOW = [
  ['Quoted', 'Paid'], ['Confirmed', 'Truck found'], ['Assigned', 'Pickup'], ['InTransit', 'In transit'], ['Delivered', 'Delivered'], ['Completed', 'Completed']
] as const;
const ORDER = ['QuotePending', 'Quoted', 'Confirmed', 'Assigned', 'InTransit', 'Delivered', 'Completed'];
const EVENT_TEXT: Record<string, string> = {
  Assigned: 'Truck and driver assigned', EnRouteToPickup: 'Driver is on the way to pickup', ReachedPickup: 'Driver reached pickup',
  PickupOtpVerified: 'Goods loaded (pickup code confirmed)', GoodsPhotoUploaded: 'Photo of loaded goods added', TripStarted: 'Trip started',
  ReachedDestination: 'Truck reached the delivery address', PodUploaded: 'Signed delivery receipt uploaded', DeliveryOtpVerified: 'Delivered (delivery code confirmed)',
  PodApproved: 'Delivery proof approved', Cancelled: 'Trip cancelled'
};

export function BookingDetailPage({ id }: { id: string }) {
  const toast = useToast();
  const s = useLoad(() => api<BookingDetail>(`/bookings/${id}`), [id]);
  const act = useAction();
  const [paying, setPaying] = useState(false);
  const [method, setMethod] = useState('UPI');
  const [cancelling, setCancelling] = useState(false);
  const [reason, setReason] = useState('');
  const b = s.data;

  const pay = () => act.run(async () => {
    await api(`/bookings/${id}/pay`, { body: { method } });
    setPaying(false); toast('Payment received. We are finding a truck for you.'); s.reload();
  });
  const requote = () => act.run(async () => { await api(`/bookings/${id}/requote`, { method: 'POST' }); toast('Fresh quote ready.'); s.reload(); });
  const cancel = () => act.run(async () => {
    await api(`/bookings/${id}/cancel`, { body: { reason } });
    setCancelling(false); toast('Booking cancelled.'); s.reload();
  });

  if (!b) return <Loading state={s} />;
  const idx = ORDER.indexOf(b.status);
  const canCancel = ['QuotePending', 'Quoted', 'Confirmed', 'Assigned'].includes(b.status) && !(b.trip && !['Assigned', 'EnRouteToPickup', 'AtPickup'].includes(b.trip.status));

  return (
    <>
      <PageHead title={`Booking ${b.bookingNumber}`} sub={<>{b.pickup.city} to {b.drop.city} · pickup {date(b.pickupDate)}{b.pickupSlot ? `, ${b.pickupSlot}` : ''}</>}>
        <Pill status={b.status} />
      </PageHead>

      {b.status !== 'Cancelled' && (
        <div className="card" style={{ marginBottom: 16 }}>
          <ol className="steps" aria-label="Booking progress">
            {FLOW.map(([code, label]) => {
              const i = ORDER.indexOf(code);
              const cls = idx > i || b.status === 'Completed' ? 'done' : idx === i ? 'now' : '';
              return <li key={code} className={cls}>{label}</li>;
            })}
          </ol>
        </div>
      )}
      {b.status === 'Cancelled' && <div style={{ marginBottom: 16 }}><Alert kind="warn" title="This booking was cancelled">{b.cancelledReason}{b.payment?.status === 'Refunded' && ' Your payment has been refunded.'}</Alert></div>}

      <div className="grid split">
        <div className="stack">
          {b.status === 'Quoted' && b.quote && (
            <div className="card">
              <div className="card-head"><h2>Pay to confirm</h2>{b.quote.expired ? <span className="pill red">Quote expired</span> : <span className="muted small">Valid until {dateTime(b.quote.validUntil)}</span>}</div>
              <p className="muted" style={{ marginBottom: 14 }}>Your money is held safely and released to the truck owner only after delivery is confirmed.</p>
              {b.quote.expired
                ? <Button onClick={requote} busy={act.busy}>Get a fresh quote</Button>
                : <Button size="lg" onClick={() => setPaying(true)}>Pay {inr(b.quote.totalAmount)}</Button>}
            </div>
          )}
          {b.status === 'Confirmed' && <Alert kind="info" title="Finding a truck">Verified owners near {b.pickup.city} can see your load now. You'll see the truck and driver here as soon as one accepts.</Alert>}

          {b.trip && (
            <div className="card">
              <div className="card-head"><h2>Truck and driver</h2><Pill status={b.trip.status} /></div>
              <div className="kv" style={{ marginBottom: 16 }}>
                <div><small>Vehicle</small><b className="mono">{b.trip.vehicle}</b></div>
                <div><small>Trip</small><b className="mono">{b.trip.tripNumber}</b></div>
                <div><small>Driver</small><b>{b.trip.driverName}</b></div>
                <div><small>Driver phone</small><b className="mono" style={{ userSelect: 'all' }}>{b.trip.driverMobile}</b></div>
              </div>
              <div className="grid g2">
                {b.trip.pickupOtp && <div className="code-box"><small>Pickup code: give it to the driver when goods are loaded</small><b>{b.trip.pickupOtp}</b></div>}
                {b.trip.deliveryOtp && <div className="code-box"><small>Delivery code: share with the receiver only</small><b>{b.trip.deliveryOtp}</b></div>}
              </div>
              {b.trip.events.length > 0 && <>
                <div className="divider" />
                <h3 style={{ marginBottom: 12 }}>Timeline</h3>
                <ul className="timeline">
                  {b.trip.events.map((e, i) => <li key={i}><b>{EVENT_TEXT[e.eventType] ?? e.eventType}</b><time>{dateTime(e.createdAt)}</time></li>)}
                </ul>
              </>}
            </div>
          )}

          <div className="card">
            <h2 style={{ marginBottom: 14 }}>Shipment</h2>
            <div className="kv">
              <div><small>Pickup</small><b>{b.pickup.address}</b><div className="muted small">{b.pickup.city}{b.pickup.contact ? ` · ${b.pickup.contact}` : ''}{b.pickup.phone ? ` · ${b.pickup.phone}` : ''}</div></div>
              <div><small>Delivery</small><b>{b.drop.address}</b><div className="muted small">{b.drop.city}{b.drop.contact ? ` · ${b.drop.contact}` : ''}{b.drop.phone ? ` · ${b.drop.phone}` : ''}</div></div>
              <div><small>Goods</small><b>{b.goodsDescription}</b><div className="muted small">{b.goods} · {kg(b.weightKg)}</div></div>
              <div><small>Vehicle type</small><b>{b.vehicleType}</b></div>
              {b.specialInstructions && <div style={{ gridColumn: '1 / -1' }}><small>Instructions</small><b>{b.specialInstructions}</b></div>}
            </div>
          </div>
        </div>

        <aside className="stack">
          {b.quote && (
            <div className="card">
              <h2 style={{ marginBottom: 8 }}>Price</h2>
              <div className="money-rows">
                <div><span>Distance</span><b className="tnum">{b.quote.distanceKm} km</b></div>
                <div><span>Vehicle</span><span>{inr(b.quote.vehicleCost)}</span></div>
                <div><span>Driver</span><span>{inr(b.quote.driverCost)}</span></div>
                <div><span>GST</span><span>{inr(b.quote.taxAmount)}</span></div>
                <div className="total"><span>Total</span><b>{inr(b.quote.totalAmount)}</b></div>
              </div>
              {b.payment && <p className="muted small" style={{ marginTop: 10 }}>{b.payment.status === 'Refunded' ? 'Refunded' : 'Paid'} by {b.payment.method} · {dateTime(b.payment.paidAt)}{b.payment.gateway === 'Test' ? ' · test payment' : ''}</p>}
            </div>
          )}
          {b.invoice && (
            <div className="card">
              <h2 style={{ marginBottom: 8 }}>Tax invoice</h2>
              <p className="mono">{b.invoice.invoiceNumber}</p>
              <div className="money-rows">
                <div><span>Taxable value</span><span>{inr(b.invoice.taxableAmount)}</span></div>
                {b.invoice.igst > 0 ? <div><span>IGST</span><span>{inr(b.invoice.igst)}</span></div> : <><div><span>CGST</span><span>{inr(b.invoice.cgst)}</span></div><div><span>SGST</span><span>{inr(b.invoice.sgst)}</span></div></>}
                <div><span><b>Total</b></span><b>{inr(b.invoice.totalAmount)}</b></div>
              </div>
            </div>
          )}
          {canCancel && <Button variant="danger" onClick={() => setCancelling(true)}>Cancel booking</Button>}
          <p className="muted small">Need help? Call support on <span className="mono" style={{ userSelect: 'all' }}>+91 00000 00000</span> with your booking number.</p>
        </aside>
      </div>

      {paying && b.quote && (
        <Dialog title={`Pay ${inr(b.quote.totalAmount)}`} onClose={() => setPaying(false)}>
          <Field label="Payment method">
            <select className="input" value={method} onChange={(e) => setMethod(e.target.value)}>
              <option value="UPI">UPI (GPay, PhonePe, Paytm)</option><option value="CreditCard">Credit card</option><option value="DebitCard">Debit card</option>
              <option value="NetBanking">Net banking</option><option value="Wallet">Wallet</option>
            </select>
          </Field>
          <Alert kind="info">Test mode: no money moves. Once your payment gateway keys are added, this opens the real checkout.</Alert>
          {act.error && <Alert kind="error">{act.error}</Alert>}
          <Button size="lg" block busy={act.busy} onClick={pay}>Pay securely</Button>
        </Dialog>
      )}
      {cancelling && (
        <Dialog title="Cancel this booking?" onClose={() => setCancelling(false)}>
          <p className="muted">{b.payment ? 'Your payment will be refunded in full.' : 'Nothing has been charged yet.'}</p>
          <Field label="Reason (optional)"><input className="input" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Plans changed" /></Field>
          {act.error && <Alert kind="error">{act.error}</Alert>}
          <div className="row end"><Button variant="secondary" onClick={() => setCancelling(false)}>Keep booking</Button><Button variant="danger" busy={act.busy} onClick={cancel}>Cancel booking</Button></div>
        </Dialog>
      )}
    </>
  );
}
