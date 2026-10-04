/**
 * Every page, who may open it, and the layout around it.
 *
 *   /login, /register          signed-out only
 *   /customer/*, /owner/*, …   signed in, and only for that role
 *
 * The API checks the same rules on every request; these guards only decide what the portal shows.
 */
import { Navigate, Route, Routes } from 'react-router';
import { ProtectedRoute, PublicOnlyRoute, RoleProtectedRoute, useAuth } from '../auth';
import { EmptyCard } from '../components';
import { AppShell } from '../layouts/AppShell';
import { AdminBookingsPage } from '../pages/admin/AdminBookingsPage';
import { AdminDocumentsPage } from '../pages/admin/AdminDocumentsPage';
import { AdminHomePage } from '../pages/admin/AdminHomePage';
import { AdminTripsPage } from '../pages/admin/AdminTripsPage';
import { ApprovalsPage } from '../pages/admin/ApprovalsPage';
import { PaymentsPage } from '../pages/admin/PaymentsPage';
import { SettlementsPage } from '../pages/admin/SettlementsPage';
import { UsersPage } from '../pages/admin/UsersPage';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { BookingDetailPage } from '../pages/customer/BookingDetailPage';
import { BookingsPage } from '../pages/customer/BookingsPage';
import { NewBookingPage } from '../pages/customer/NewBookingPage';
import { DriverHomePage } from '../pages/driver/DriverHomePage';
import { DriversPage } from '../pages/owner/DriversPage';
import { OwnerHomePage } from '../pages/owner/OwnerHomePage';
import { OwnerTripsPage } from '../pages/owner/OwnerTripsPage';
import { PayoutsPage } from '../pages/owner/PayoutsPage';
import { VehiclesPage } from '../pages/owner/VehiclesPage';
import { MyDocumentsPage } from '../pages/shared/MyDocumentsPage';
import { HOME_PAGES } from './navigation';

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route index element={<HomeRedirect />} />

        <Route element={<AppShell />}>
          <Route element={<RoleProtectedRoute roles={['Customer']} />}>
            <Route path="/customer/book" element={<NewBookingPage />} />
            <Route path="/customer/bookings" element={<BookingsPage />} />
            <Route path="/customer/bookings/:id" element={<BookingDetailPage />} />
          </Route>

          <Route element={<RoleProtectedRoute roles={['Owner']} />}>
            <Route path="/owner" element={<OwnerHomePage />} />
            <Route path="/owner/vehicles" element={<VehiclesPage />} />
            <Route path="/owner/drivers" element={<DriversPage />} />
            <Route path="/owner/trips" element={<OwnerTripsPage />} />
            <Route path="/owner/payouts" element={<PayoutsPage />} />
            <Route path="/owner/documents" element={<MyDocumentsPage />} />
          </Route>

          <Route element={<RoleProtectedRoute roles={['Driver']} />}>
            <Route path="/driver" element={<DriverHomePage />} />
            <Route path="/driver/documents" element={<MyDocumentsPage />} />
          </Route>

          <Route element={<RoleProtectedRoute roles={['Admin']} />}>
            <Route path="/admin" element={<AdminHomePage />} />
            <Route path="/admin/approvals" element={<ApprovalsPage />} />
            <Route path="/admin/documents" element={<AdminDocumentsPage />} />
            <Route path="/admin/bookings" element={<AdminBookingsPage />} />
            <Route path="/admin/trips" element={<AdminTripsPage />} />
            <Route path="/admin/payments" element={<PaymentsPage />} />
            <Route path="/admin/settlements" element={<SettlementsPage />} />
            <Route path="/admin/users" element={<UsersPage />} />
          </Route>

          <Route
            path="*"
            element={<EmptyCard title="Page not found">Use the menu to find what you need.</EmptyCard>}
          />
        </Route>
      </Route>
    </Routes>
  );
}

/** "/" goes to the signed-in user's home page. */
function HomeRedirect() {
  const { user } = useAuth();
  return <Navigate to={user ? HOME_PAGES[user.role] : '/login'} replace />;
}
