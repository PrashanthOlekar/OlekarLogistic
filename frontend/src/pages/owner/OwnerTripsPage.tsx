import { tripsApi } from '../../api/tripsApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill, RouteLabel } from '../../components';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { date, inr } from '../../utils/format';

export function OwnerTripsPage() {
  const trips = usePagedLoad((page) => tripsApi.list({ ...page, sort: 'Newest' }));

  return (
    <>
      <PageHead title="Trips" sub="Trips your vehicles are running or have completed." />

      <Loading state={trips} />

      {trips.items?.length === 0 && (
        <EmptyCard title="No trips yet">Take a load from the overview page to start.</EmptyCard>
      )}

      {!!trips.items?.length && (
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
              {trips.items.map((trip) => (
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
          <Pagination page={trips.data} onPageChange={trips.setPageNumber} />
        </div>
      )}
    </>
  );
}
