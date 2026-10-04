import { Link, useNavigate } from 'react-router';
import { bookingsApi } from '../../api/bookingsApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill, RouteLabel } from '../../components';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { date, inr, kg } from '../../utils/format';

export function BookingsPage() {
  const navigate = useNavigate();
  const bookings = usePagedLoad((page) => bookingsApi.list({ ...page, sort: 'Newest' }));

  return (
    <>
      <PageHead title="My bookings" sub="Every trip you've booked, newest first.">
        <Link to="/customer/book" className="btn btn-primary">
          Book a truck
        </Link>
      </PageHead>

      <Loading state={bookings} />

      {bookings.items?.length === 0 && (
        <EmptyCard title="No bookings yet">Book your first truck and it will appear here.</EmptyCard>
      )}

      {!!bookings.items?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Booking</th>
                <th>Route</th>
                <th>Pickup</th>
                <th>Vehicle</th>
                <th className="num">Weight</th>
                <th className="num">Total</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {bookings.items.map((booking) => (
                <tr
                  key={booking.id}
                  className="link"
                  onClick={() => navigate(`/customer/bookings/${booking.id}`)}
                >
                  <td>
                    <Link to={`/customer/bookings/${booking.id}`} className="mono">
                      {booking.bookingNumber}
                    </Link>
                  </td>
                  <td>
                    <RouteLabel from={booking.from} to={booking.to} />
                  </td>
                  <td>{date(booking.pickupDate)}</td>
                  <td>{booking.vehicleType}</td>
                  <td className="num">{kg(booking.weightKg)}</td>
                  <td className="num">{inr(booking.total)}</td>
                  <td>
                    <Pill status={booking.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination page={bookings.data} onPageChange={bookings.setPageNumber} />
        </div>
      )}
    </>
  );
}
