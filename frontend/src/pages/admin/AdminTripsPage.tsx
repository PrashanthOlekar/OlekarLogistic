import { useState } from 'react';
import { Alert, Button, EmptyCard, Loading, PageHead, Pill, RouteLabel } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api, openDocument } from '../../lib/api';
import { dateTime } from '../../lib/format';
import { useToast } from '../../state/ToastContext';
import { StatusFilter, withStatus } from './StatusFilter';
import type { AdminTrip } from './types';

const FILTERS: [string, string][] = [
  ['', 'All'],
  ['InTransit', 'In transit'],
  ['Delivered', 'POD to approve'],
  ['Completed', 'Completed'],
];

export function AdminTripsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('');
  const trips = useLoad(() => api<AdminTrip[]>(withStatus('/admin/trips', status)), [status]);
  const approve = useAction();

  /** Approving the POD completes the trip, issues the invoice and unlocks the owner's payout. */
  const approvePod = (trip: AdminTrip) =>
    approve.run(async () => {
      await api(`/admin/trips/${trip.id}/approve-pod`, { method: 'POST' });
      toast(`POD approved. Invoice issued and ${trip.owner}'s payout is ready to send.`);
      trips.reload();
    });

  const viewPod = (documentId: number) =>
    openDocument(documentId).catch((error) => toast(error.message, 'error'));

  return (
    <>
      <PageHead
        title="Trips & POD"
        sub="Open the delivery receipt and approve it to close the trip, issue the invoice and unlock the owner's payout."
      >
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      {approve.error && (
        <Alert kind="error" className="mb-sm">
          {approve.error}
        </Alert>
      )}

      <Loading state={trips} />

      {trips.data?.length === 0 && <EmptyCard title="No trips" />}

      {!!trips.data?.length && (
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
              {trips.data.map((trip) => (
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
        </div>
      )}
    </>
  );
}
