import { useState } from 'react';
import { Alert, Button, Loading, MoneyRow, MoneyRows, PageHead, Pill } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date, dateTime, inr, kg } from '../../lib/format';
import { useToast } from '../../state/ToastContext';
import {
  CANCELLABLE_BOOKING,
  CANCELLABLE_TRIP,
  EVENT_TEXT,
  PROGRESS_STEPS,
  progressStepClass,
} from './bookingProgress';
import { CancelBookingDialog, PayDialog } from './BookingDialogs';
import type { Stop } from '../../lib/types';
import type { BookingDetail, BookingInvoice, BookingTrip } from './types';

const SUPPORT_PHONE = '+91 00000 00000';

export function BookingDetailPage({ id }: { id: string }) {
  const toast = useToast();
  const booking = useLoad(() => api<BookingDetail>(`/bookings/${id}`), [id]);
  const requote = useAction();
  const [paying, setPaying] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  const b = booking.data;
  if (!b) {
    return <Loading state={booking} />;
  }

  const getFreshQuote = () =>
    requote.run(async () => {
      await api(`/bookings/${id}/requote`, { method: 'POST' });
      toast('Fresh quote ready.');
      booking.reload();
    });

  const tripHasStarted = b.trip != null && !CANCELLABLE_TRIP.includes(b.trip.status);
  const canCancel = CANCELLABLE_BOOKING.includes(b.status) && !tripHasStarted;
  const pickupSlot = b.pickupSlot ? `, ${b.pickupSlot}` : '';

  return (
    <>
      <PageHead
        title={`Booking ${b.bookingNumber}`}
        sub={`${b.pickup.city} to ${b.drop.city} · pickup ${date(b.pickupDate)}${pickupSlot}`}
      >
        <Pill status={b.status} />
      </PageHead>

      {b.status === 'Cancelled' ? (
        <Alert kind="warn" title="This booking was cancelled" className="mb-md">
          {b.cancelledReason}
          {b.payment?.status === 'Refunded' && ' Your payment has been refunded.'}
        </Alert>
      ) : (
        <div className="card mb-md">
          <ol className="steps" aria-label="Booking progress">
            {PROGRESS_STEPS.map(([stepStatus, label]) => (
              <li key={stepStatus} className={progressStepClass(b.status, stepStatus)}>
                {label}
              </li>
            ))}
          </ol>
        </div>
      )}

      <div className="grid split">
        <div className="stack">
          {b.status === 'Quoted' && b.quote && (
            <div className="card">
              <div className="card-head">
                <h2>Pay to confirm</h2>
                {b.quote.expired ? (
                  <span className="pill red">Quote expired</span>
                ) : (
                  <span className="muted small">Valid until {dateTime(b.quote.validUntil)}</span>
                )}
              </div>
              <p className="muted mb-md">
                Your money is held safely and released to the truck owner only after delivery is confirmed.
              </p>
              {requote.error && (
                <Alert kind="error" className="mb-md">
                  {requote.error}
                </Alert>
              )}
              {b.quote.expired ? (
                <Button onClick={getFreshQuote} busy={requote.busy}>
                  Get a fresh quote
                </Button>
              ) : (
                <Button size="lg" onClick={() => setPaying(true)}>
                  Pay {inr(b.quote.totalAmount)}
                </Button>
              )}
            </div>
          )}

          {b.status === 'Confirmed' && (
            <Alert kind="info" title="Finding a truck">
              Verified owners near {b.pickup.city} can see your load now. You'll see the truck and driver here
              as soon as one accepts.
            </Alert>
          )}

          {b.trip && <TruckCard trip={b.trip} />}

          <ShipmentCard booking={b} />
        </div>

        <aside className="stack">
          {b.quote && (
            <div className="card">
              <h2 className="card-title">Price</h2>
              <MoneyRows>
                <MoneyRow label="Distance" value={<b className="tnum">{b.quote.distanceKm} km</b>} />
                <MoneyRow label="Vehicle" value={inr(b.quote.vehicleCost)} />
                <MoneyRow label="Driver" value={inr(b.quote.driverCost)} />
                <MoneyRow label="GST" value={inr(b.quote.taxAmount)} />
                <MoneyRow label="Total" value={inr(b.quote.totalAmount)} total />
              </MoneyRows>
              {b.payment && (
                <p className="muted small mt-md">
                  {b.payment.status === 'Refunded' ? 'Refunded' : 'Paid'} by {b.payment.method} ·{' '}
                  {dateTime(b.payment.paidAt)}
                  {b.payment.gateway === 'Test' && ' · test payment'}
                </p>
              )}
            </div>
          )}

          {b.invoice && <InvoiceCard invoice={b.invoice} />}

          {canCancel && (
            <Button variant="danger" onClick={() => setCancelling(true)}>
              Cancel booking
            </Button>
          )}

          <p className="muted small">
            Need help? Call support on <span className="mono selectable">{SUPPORT_PHONE}</span> with your
            booking number.
          </p>
        </aside>
      </div>

      {paying && b.quote && (
        <PayDialog
          bookingId={id}
          amount={b.quote.totalAmount}
          onClose={() => setPaying(false)}
          onPaid={() => {
            setPaying(false);
            toast('Payment received. We are finding a truck for you.');
            booking.reload();
          }}
        />
      )}

      {cancelling && (
        <CancelBookingDialog
          bookingId={id}
          isPaid={!!b.payment}
          onClose={() => setCancelling(false)}
          onCancelled={() => {
            setCancelling(false);
            toast('Booking cancelled.');
            booking.reload();
          }}
        />
      )}
    </>
  );
}

/** Vehicle, driver, the two handover codes and the trip timeline. */
function TruckCard({ trip }: { trip: BookingTrip }) {
  return (
    <div className="card">
      <div className="card-head">
        <h2>Truck and driver</h2>
        <Pill status={trip.status} />
      </div>

      <div className="kv mb-md">
        <div>
          <small>Vehicle</small>
          <b className="mono">{trip.vehicle}</b>
        </div>
        <div>
          <small>Trip</small>
          <b className="mono">{trip.tripNumber}</b>
        </div>
        <div>
          <small>Driver</small>
          <b>{trip.driverName}</b>
        </div>
        <div>
          <small>Driver phone</small>
          <b className="mono selectable">{trip.driverMobile}</b>
        </div>
      </div>

      <div className="grid g2">
        {trip.pickupOtp && (
          <div className="code-box">
            <small>Pickup code: give it to the driver when goods are loaded</small>
            <b>{trip.pickupOtp}</b>
          </div>
        )}
        {trip.deliveryOtp && (
          <div className="code-box">
            <small>Delivery code: share with the receiver only</small>
            <b>{trip.deliveryOtp}</b>
          </div>
        )}
      </div>

      {trip.events.length > 0 && (
        <>
          <div className="divider" />
          <h3 className="card-title">Timeline</h3>
          <ul className="timeline">
            {trip.events.map((tripEvent, index) => (
              <li key={index}>
                <b>{EVENT_TEXT[tripEvent.eventType] ?? tripEvent.eventType}</b>
                <time>{dateTime(tripEvent.createdAt)}</time>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

function ShipmentCard({ booking }: { booking: BookingDetail }) {
  return (
    <div className="card">
      <h2 className="card-title">Shipment</h2>
      <div className="kv">
        <StopDetail title="Pickup" stop={booking.pickup} />
        <StopDetail title="Delivery" stop={booking.drop} />
        <div>
          <small>Goods</small>
          <b>{booking.goodsDescription}</b>
          <div className="muted small">
            {booking.goods} · {kg(booking.weightKg)}
          </div>
        </div>
        <div>
          <small>Vehicle type</small>
          <b>{booking.vehicleType}</b>
        </div>
        {booking.specialInstructions && (
          <div className="wide">
            <small>Instructions</small>
            <b>{booking.specialInstructions}</b>
          </div>
        )}
      </div>
    </div>
  );
}

function StopDetail({ title, stop }: { title: string; stop: Stop }) {
  const details = [stop.city, stop.contact, stop.phone].filter(Boolean).join(' · ');
  return (
    <div>
      <small>{title}</small>
      <b>{stop.address}</b>
      <div className="muted small">{details}</div>
    </div>
  );
}

function InvoiceCard({ invoice }: { invoice: BookingInvoice }) {
  const isInterState = invoice.igst > 0;
  return (
    <div className="card">
      <h2 className="card-title">Tax invoice</h2>
      <p className="mono">{invoice.invoiceNumber}</p>
      <MoneyRows>
        <MoneyRow label="Taxable value" value={inr(invoice.taxableAmount)} />
        {isInterState ? (
          <MoneyRow label="IGST" value={inr(invoice.igst)} />
        ) : (
          <>
            <MoneyRow label="CGST" value={inr(invoice.cgst)} />
            <MoneyRow label="SGST" value={inr(invoice.sgst)} />
          </>
        )}
        <MoneyRow label={<b>Total</b>} value={<b>{inr(invoice.totalAmount)}</b>} />
      </MoneyRows>
    </div>
  );
}
