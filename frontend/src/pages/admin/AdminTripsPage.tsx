import { useState } from 'react';
import { documentsApi } from '../../api/documentsApi';
import { tripsApi } from '../../api/tripsApi';
import {
  Alert,
  Button,
  EmptyCard,
  Loading,
  PageHead,
  Pagination,
  Pill,
  RouteLabel,
  useToast,
} from '../../components';
import { SearchBox } from '../../features/admin/SearchBox';
import { StatusFilter } from '../../features/admin/StatusFilter';
import { useAction } from '../../hooks/useAction';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import type { TripListItem } from '../../types';
import { dateTime } from '../../utils/format';

const FILTERS: [string, string][] = [
  ['', 'All'],
  ['InTransit', 'In transit'],
  ['Delivered', 'POD to approve'],
  ['Completed', 'Completed'],
];

export function AdminTripsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const trips = usePagedLoad(
    (page) => tripsApi.list({ ...page, status, search, sort: 'Newest' }),
    [status, search],
  );
  const approve = useAction();

  /** Approving the POD completes the trip, issues the invoice and unlocks the owner's payout. */
  const approvePod = (trip: TripListItem) =>
    approve.run(async () => {
      await tripsApi.approvePod(trip.id);
      toast(`POD approved. Invoice issued and ${trip.owner}'s payout is ready to send.`);
      trips.reload();
    });

  const viewPod = (documentId: number) =>
    documentsApi.open(documentId).catch((error) => toast(error.message, 'error'));

  return (
    <>
      <PageHead
        title="Trips & POD"
        sub="Open the delivery receipt and approve it to close the trip, issue the invoice and unlock the owner's payout."
      >
        <SearchBox placeholder="Trip, booking or vehicle no." onSearch={setSearch} />
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      {approve.error && (
        <Alert kind="error" className="mb-sm">
          {approve.error}
        </Alert>
      )}

      <Loading state={trips} />

      {trips.items?.length === 0 && <EmptyCard title="No trips" />}

      {!!trips.items?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Trip</th>
                <th>Route</th>
                <th>Vehicle</th>
                <th>Driver</th>
                <th>Owner</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {trips.items.map((trip) => (
                <tr key={trip.id}>
                  <td className="mono">
                    {trip.tripNumber}
                    <div className="small muted">{trip.bookingNumber}</div>
                  </td>
                  <td>
                    <RouteLabel from={trip.from} to={trip.to} />
                  </td>
                  <td className="mono">{trip.vehicle}</td>
                  <td>
                    {trip.driver}
                    <div className="mono small muted">{trip.driverMobile}</div>
                  </td>
                  <td>{trip.owner}</td>
                  <td>
                    <Pill status={trip.status} />
                    {trip.deliveredAt && <div className="small muted">{dateTime(trip.deliveredAt)}</div>}
                  </td>
                  <td>
                    {trip.status === 'Delivered' && (
                      <div className="row tight">
                        {trip.pod && (
                          <Button size="sm" variant="secondary" onClick={() => viewPod(trip.pod!)}>
                            View POD
                          </Button>
                        )}
                        <Button
                          size="sm"
                          variant="success"
                          busy={approve.busy}
                          onClick={() => approvePod(trip)}
                        >
                          Approve POD
                        </Button>
                      </div>
                    )}
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
