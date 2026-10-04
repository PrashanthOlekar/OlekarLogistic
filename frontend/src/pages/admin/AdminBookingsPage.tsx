import { useState } from 'react';
import { bookingsApi } from '../../api/bookingsApi';
import { Button, EmptyCard, Loading, PageHead, Pagination, Pill, RouteLabel, useToast } from '../../components';
import { SearchBox } from '../../features/admin/SearchBox';
import { StatusFilter } from '../../features/admin/StatusFilter';
import { AssignTruckDialog } from '../../features/fleet/AssignTruckDialog';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import type { BookingListItem } from '../../types';
import { date, inr, kg } from '../../utils/format';

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
  const [search, setSearch] = useState('');
  const bookings = usePagedLoad((page) => bookingsApi.list({ ...page, status, search }), [status, search]);
  const [assigning, setAssigning] = useState<BookingListItem | null>(null);

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
        <SearchBox placeholder="Booking no., customer or mobile" onSearch={setSearch} />
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      <Loading state={bookings} />

      {bookings.items?.length === 0 && <EmptyCard title="No bookings" />}

      {!!bookings.items?.length && (
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
              {bookings.items.map((booking) => (
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
          <Pagination page={bookings.data} onPageChange={bookings.setPageNumber} />
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
