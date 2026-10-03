import { EmptyCard, Loading, PageHead, Pill, RouteLabel } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date, inr } from '../../lib/format';
import type { OwnerTrip } from './types';

export function OwnerTripsPage() {
  const trips = useLoad(() => api<OwnerTrip[]>('/owner/trips'));

  return (
    <>
      <PageHead title="Trips" sub="Trips your vehicles are running or have completed." />

      <Loading state={trips} />

      {trips.data?.length === 0 && (
        <EmptyCard title="No trips yet">Take a load from the overview page to start.</EmptyCard>
      )}

      {!!trips.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Trip</th>
                <th>Route</th>
                <th>Pickup</th>
                <th>Vehicle</th>
                <th>Driver</th>
                <th className="num">Your payout</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {trips.data.map((trip) => (
                <tr key={trip.id}>
                  <td className="mono">{trip.tripNumber}</td>
                  <td>
                    <RouteLabel from={trip.from} to={trip.to} />
                  </td>
                  <td>{date(trip.pickupDate)}</td>
                  <td className="mono">{trip.vehicle}</td>
                  <td>{trip.driver}</td>
                  <td className="num">{inr(trip.ownerPayout)}</td>
                  <td>
                    <Pill status={trip.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
