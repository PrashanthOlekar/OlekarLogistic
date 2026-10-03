import { useState } from 'react';
import { EmptyCard, Loading, PageHead, Pill, RouteLabel } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date } from '../../lib/format';
import { TripRunner } from './TripRunner';
import { FINISHED_STATUSES } from './tripSteps';
import type { DriverTripRow } from './types';

export function DriverHomePage() {
  const trips = useLoad(() => api<DriverTripRow[]>('/driver/trips'));
  const [chosenTripId, setChosenTripId] = useState<number | null>(null);

  // Show the trip the driver picked, else the one in progress, else the latest
  // (so a delivery that has just finished stays on screen).
  const activeTrip = trips.data?.find((trip) => !FINISHED_STATUSES.includes(trip.status));
  const shownTripId = chosenTripId ?? activeTrip?.id ?? trips.data?.[0]?.id ?? null;

  return (
    <>
      <PageHead
        title="My trips"
        sub="One big button for the next step. Your owner and the customer see each step as you tap it."
      />

      <Loading state={trips} />

      {trips.data?.length === 0 && (
        <EmptyCard title="No trips assigned yet">
          When your owner takes a load for you, it appears here.
        </EmptyCard>
      )}

      <div className="grid split">
        <div className="driver-wrap">
          {shownTripId && <TripRunner tripId={shownTripId} onChanged={trips.reload} />}
        </div>

        {!!trips.data?.length && (
          <div className="card">
            <h2 className="card-title">All trips</h2>
            <div className="stack compact">
              {trips.data.map((trip) => (
                <button
                  key={trip.id}
                  type="button"
                  className="card card-compact trip-pick"
                  aria-current={trip.id === shownTripId}
                  onClick={() => setChosenTripId(trip.id)}
                >
                  <div className="row between">
                    <RouteLabel from={trip.from} to={trip.to} />
                    <Pill status={trip.status} />
                  </div>
                  <div className="muted small">
                    {trip.tripNumber} · {date(trip.pickupDate)} · {trip.vehicle}
                  </div>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </>
  );
}
