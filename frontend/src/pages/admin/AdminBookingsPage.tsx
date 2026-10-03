import { useState } from 'react';
import { Button, EmptyCard, Loading, PageHead, Pill, RouteLabel } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date, inr, kg } from '../../lib/format';
import { useToast } from '../../state/ToastContext';
import { AssignTruckDialog } from './AssignTruckDialog';
import { StatusFilter, withStatus } from './StatusFilter';
import type { AdminBooking } from './types';

const FILTERS: [string, string][] = [
  ['', 'All'],
  ['Quoted', 'Unpaid'],
  ['Confirmed', 'Needs truck'],
  ['Assigned', 'Assigned'],
  ['InTransit', 'In transit'],
  ['Delivered', 'Delivered'],
];

export function AdminBookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('');
  const bookings = useLoad(() => api<AdminBooking[]>(withStatus('/admin/bookings', status)), [status]);
  const [assigning, setAssigning] = useState<AdminBooking | null>(null);

  const handleAssigned = (tripNumber: string) => {
    toast(`Assigned. Trip ${tripNumber} created.`);
    setAssigning(null);
    bookings.reload();
  };

  return (
    <>
      <PageHead
        title="Bookings"
        sub="Paid bookings are offered to owners automatically. Assign one yourself if no owner takes it."
      >
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      <Loading state={bookings} />

      {bookings.data?.length === 0 && <EmptyCard title="No bookings" />}

      {!!bookings.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Booking</th>
                <th>Customer</th>
                <th>Route</th>
                <th>Pickup</th>
                <th>Load</th>
                <th className="num">Total</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {bookings.data.map((booking) => (
                <tr key={booking.id}>
                  <td className="mono">{booking.bookingNumber}</td>
                  <td>
                    <b>{booking.customer}</b>
                    <div className="mono small muted">{booking.customerMobile}</div>
                  </td>
                  <td>
                    <RouteLabel from={booking.from} to={booking.to} />
                  </td>
                  <td>{date(booking.pickupDate)}</td>
                  <td className="small">
                    {booking.vehicleType}
                    <br />
                    {kg(booking.weightKg)}
                  </td>
                  <td className="num">{inr(booking.total)}</td>
                  <td>
                    <Pill status={booking.status} />
                  </td>
                  <td>
                    {booking.status === 'Confirmed' && (
                      <Button size="sm" variant="dark" onClick={() => setAssigning(booking)}>
                        Assign truck
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {assigning && (
        <AssignTruckDialog
          booking={assigning}
          onClose={() => setAssigning(null)}
          onAssigned={handleAssigned}
        />
      )}
    </>
  );
}
