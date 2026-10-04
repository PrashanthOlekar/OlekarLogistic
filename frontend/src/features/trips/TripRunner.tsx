/** The driver's screen for one trip: the next big button, the checklist and the addresses. */
import { useEffect, useState } from 'react';
import { tripsApi } from '../../api/tripsApi';
import { Alert, Button, Field, Loading, Pill, useToast } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { directionsUrl, getCurrentPosition } from '../../services/location';
import type { HandoverKind, Stop, TripAction, TripPhotoKind } from '../../types';
import { date, dateTime, kg } from '../../utils/format';
import { STEP_LABELS, TripStep, currentStep } from './tripSteps';

interface TripRunnerProps {
  tripId: number;
  /** Called after every step so the trip list refreshes too. */
  onChanged: () => void;
}

export function TripRunner({ tripId, onChanged }: TripRunnerProps) {
  const toast = useToast();
  const trip = useLoad(() => tripsApi.get(tripId), [tripId]);
  const action = useAction();
  const [code, setCode] = useState('');
  const [file, setFile] = useState<File | null>(null);

  // Clear the code and photo whenever the trip moves on.
  useEffect(() => {
    setCode('');
    setFile(null);
  }, [tripId, trip.data?.status]);

  const t = trip.data;
  if (!t) {
    return <Loading state={trip} />;
  }

  const step = currentStep(t);
  const booking = t.booking;

  const refresh = () => {
    trip.reload();
    onChanged();
  };

  const advance = (tripAction: TripAction, message: string) =>
    action.run(async () => {
      const position = await getCurrentPosition();
      await tripsApi.recordEvent(tripId, { action: tripAction, ...position });
      toast(message);
      refresh();
    });

  const verifyCode = (kind: HandoverKind) =>
    action.run(async () => {
      await tripsApi.confirmHandover(tripId, kind, code);
      toast(kind === 'Pickup' ? 'Code correct. Goods loaded.' : 'Delivered. Well done!');
      refresh();
    });

  const uploadPhoto = (kind: TripPhotoKind) =>
    action.run(async () => {
      const position = await getCurrentPosition();
      await tripsApi.uploadPhoto(tripId, kind, file!, position);
      toast(kind === 'POD' ? 'Delivery receipt uploaded.' : 'Photo saved.');
      setFile(null);
      refresh();
    });

  const codeInput = (
    <input
      className="input otp-input"
      inputMode="numeric"
      maxLength={4}
      value={code}
      onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))}
      autoFocus
    />
  );

  const distance = t.plannedDistanceKm ? ` · ${t.plannedDistanceKm} km` : '';
  const busy = action.busy;

  return (
    <div className="stack">
      <div className="card trip-hero">
        <div className="row between">
          <span className="mono">
            {t.tripNumber} · {t.vehicle}
          </span>
          <Pill status={t.status} />
        </div>
        <h2>
          {booking.pickup.city} → {booking.drop.city}
        </h2>
        <p>
          {booking.goodsDescription} · {kg(booking.weightKg)}
          {distance}
        </p>
      </div>

      {action.error && <Alert kind="error">{action.error}</Alert>}

      {step === TripStep.GoToPickup && (
        <>
          <a
            className="big-btn navy"
            href={directionsUrl(booking.pickup.address, booking.pickup.city)}
            target="_blank"
            rel="noreferrer"
          >
            Open map to pickup
          </a>
          <button
            className="big-btn"
            disabled={busy}
            onClick={() => advance('EnRoute', 'Customer told you are on the way.')}
          >
            I'm on the way
          </button>
        </>
      )}

      {step === TripStep.ReachPickup && (
        <button
          className="big-btn"
          disabled={busy}
          onClick={() => advance('ReachedPickup', 'Marked as reached pickup.')}
        >
          I reached pickup
        </button>
      )}

      {step === TripStep.EnterPickupCode && (
        <div className="card stack">
          <Field
            label="Pickup code from the sender"
            hint="The sender sees this 4-digit code in their booking."
          >
            {codeInput}
          </Field>
          <button
            className="big-btn"
            disabled={busy || code.length !== 4}
            onClick={() => verifyCode('Pickup')}
          >
            Confirm goods loaded
          </button>
          <Field label="Photo of loaded goods (optional)">
            <input
              className="input"
              type="file"
              accept="image/*"
              capture="environment"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          </Field>
          {file && (
            <Button variant="secondary" busy={busy} onClick={() => uploadPhoto('PickupPhoto')}>
              Save photo
            </Button>
          )}
        </div>
      )}

      {step === TripStep.StartTrip && (
        <button
          className="big-btn green"
          disabled={busy}
          onClick={() => advance('StartTrip', 'Trip started. Drive safe!')}
        >
          Start trip
        </button>
      )}

      {step === TripStep.ReachDestination && (
        <>
          <a
            className="big-btn navy"
            href={directionsUrl(booking.drop.address, booking.drop.city)}
            target="_blank"
            rel="noreferrer"
          >
            Open map to delivery
          </a>
          <button
            className="big-btn"
            disabled={busy}
            onClick={() => advance('ReachedDestination', 'Marked as reached destination.')}
          >
            I reached the destination
          </button>
        </>
      )}

      {step === TripStep.UploadPod && (
        <div className="card stack">
          <Field label="Photo of the signed delivery receipt (POD)">
            <input
              className="input"
              type="file"
              accept="image/*,.pdf"
              capture="environment"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          </Field>
          <button className="big-btn" disabled={busy || !file} onClick={() => uploadPhoto('POD')}>
            Upload POD
          </button>
        </div>
      )}

      {step === TripStep.EnterDeliveryCode && (
        <div className="card stack">
          <Field label="Delivery code from the receiver" hint="The customer shares it with the receiver.">
            {codeInput}
          </Field>
          <button
            className="big-btn green"
            disabled={busy || code.length !== 4}
            onClick={() => verifyCode('Delivery')}
          >
            Complete delivery
          </button>
        </div>
      )}

      {step === TripStep.Finished && <FinishedMessage status={t.status} />}

      <div className="card">
        <ul className="checklist" aria-label="Trip steps">
          {STEP_LABELS.map((label, index) => (
            <li key={label} className={index < step ? 'done' : index === step ? 'now' : ''}>
              <span className="d">{index < step ? '✓' : ''}</span>
              {label}
            </li>
          ))}
        </ul>
      </div>

      <div className="card">
        <div className="kv">
          <StopDetail title="Pickup" stop={booking.pickup} />
          <StopDetail title="Delivery" stop={booking.drop} />
          <div>
            <small>Pickup date</small>
            <b>{date(booking.pickupDate)}</b>
            <div className="muted small">{booking.pickupSlot}</div>
          </div>
          <div>
            <small>Booking</small>
            <b className="mono">{booking.bookingNumber}</b>
          </div>
          {booking.specialInstructions && (
            <div className="wide">
              <small>Instructions</small>
              <b>{booking.specialInstructions}</b>
            </div>
          )}
        </div>
        {t.events.length > 0 && (
          <p className="muted small mt-md">Last update {dateTime(t.events[t.events.length - 1].createdAt)}</p>
        )}
      </div>
    </div>
  );
}

function FinishedMessage({ status }: { status: string }) {
  if (status === 'Cancelled') {
    return (
      <Alert kind="success" title="Trip cancelled">
        The customer cancelled this trip.
      </Alert>
    );
  }
  return (
    <Alert kind="success" title="Trip delivered">
      Your owner is paid after ProCargo checks the delivery receipt.
    </Alert>
  );
}

function StopDetail({ title, stop }: { title: string; stop: Stop }) {
  const contact = stop.contact ? ` · ${stop.contact}` : '';
  return (
    <div>
      <small>{title}</small>
      <b>{stop.address}</b>
      <div className="muted small">
        {stop.city}
        {contact} {stop.phone && <span className="mono">{stop.phone}</span>}
      </div>
    </div>
  );
}
