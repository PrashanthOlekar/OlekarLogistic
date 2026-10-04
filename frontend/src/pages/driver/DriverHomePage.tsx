import { useState } from 'react';
import { tripsApi } from '../../api/tripsApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill, RouteLabel } from '../../components';
import { TripRunner } from '../../features/trips/TripRunner';
import { FINISHED_STATUSES } from '../../features/trips/tripSteps';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { date } from '../../utils/format';

/** The trip in progress is listed first (sort ActiveFirst), then the newest. */
const TRIPS_PER_PAGE = 10;

export function DriverHomePage() {
  const trips = usePagedLoad((page) => tripsApi.list({ ...page, sort: 'ActiveFirst' }), [], TRIPS_PER_PAGE);
  const [chosenTripId, setChosenTripId] = useState<number | null>(null);

  // Show the trip the driver picked, else the one in progress, else the latest
  // (so a delivery that has just finished stays on screen).
  const activeTrip = trips.items?.find((trip) => !FINISHED_STATUSES.includes(trip.status));
  const shownTripId = chosenTripId ?? activeTrip?.id ?? trips.items?.[0]?.id ?? null;

  return (
    <>
      <PageHead
        title="My trips"
        sub="One big button for the next step. Your owner and the customer see each step as you tap it."
      />

      <Loading state={trips} />

      {trips.items?.length === 0 && (
        <EmptyCard title="No trips assigned yet">
          When your owner takes a load for you, it appears here.
        </EmptyCard>
      )}

      <div className="grid split">
        <div className="driver-wrap">
          {shownTripId && <TripRunner tripId={shownTripId} onChanged={trips.reload} />}
        </div>

        {!!trips.items?.length && (
          <div className="card">
            <h2 className="card-title">All trips</h2>
            <div className="stack compact">
              {trips.items.map((trip) => (
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
            <Pagination page={trips.data} onPageChange={trips.setPageNumber} />
          </div>
        )}
      </div>
    </>
  );
}
