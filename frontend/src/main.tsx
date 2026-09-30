import { StrictMode, useEffect, type ReactNode } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';
import type { Role } from './api';
import { match, navigate, usePath } from './router';
import { AuthProvider, ToastProvider, useAuth } from './state';
import { Shell } from './components/Shell';
import { LoginPage, homeFor } from './pages/Login';
import { RegisterPage } from './pages/Register';
import { NewBookingPage } from './pages/customer/NewBooking';
import { BookingDetailPage, BookingsPage } from './pages/customer/Bookings';
import { DriversPage, OwnerHomePage, OwnerTripsPage, PayoutsPage, VehiclesPage } from './pages/owner/OwnerPages';
import { MyDocumentsPage } from './pages/MyDocuments';
import { DriverHomePage } from './pages/driver/DriverHome';
import { AdminBookingsPage, AdminDocumentsPage, AdminHomePage, AdminTripsPage, ApprovalsPage, PaymentsPage, SettlementsPage, UsersPage } from './pages/admin/AdminPages';
import { Empty } from './components/ui';

type Page = (params: Record<string, string>) => ReactNode;
const ROUTES: { path: string; role: Role; page: Page }[] = [
  { path: '/customer/book', role: 'Customer', page: () => <NewBookingPage /> },
  { path: '/customer/bookings', role: 'Customer', page: () => <BookingsPage /> },
  { path: '/customer/bookings/:id', role: 'Customer', page: (p) => <BookingDetailPage id={p.id} /> },
  { path: '/owner', role: 'Owner', page: () => <OwnerHomePage /> },
  { path: '/owner/vehicles', role: 'Owner', page: () => <VehiclesPage /> },
  { path: '/owner/drivers', role: 'Owner', page: () => <DriversPage /> },
  { path: '/owner/trips', role: 'Owner', page: () => <OwnerTripsPage /> },
  { path: '/owner/payouts', role: 'Owner', page: () => <PayoutsPage /> },
  { path: '/owner/documents', role: 'Owner', page: () => <MyDocumentsPage /> },
  { path: '/driver', role: 'Driver', page: () => <DriverHomePage /> },
  { path: '/driver/documents', role: 'Driver', page: () => <MyDocumentsPage /> },
  { path: '/admin', role: 'Admin', page: () => <AdminHomePage /> },
  { path: '/admin/approvals', role: 'Admin', page: () => <ApprovalsPage /> },
  { path: '/admin/documents', role: 'Admin', page: () => <AdminDocumentsPage /> },
  { path: '/admin/bookings', role: 'Admin', page: () => <AdminBookingsPage /> },
  { path: '/admin/trips', role: 'Admin', page: () => <AdminTripsPage /> },
  { path: '/admin/payments', role: 'Admin', page: () => <PaymentsPage /> },
  { path: '/admin/settlements', role: 'Admin', page: () => <SettlementsPage /> },
  { path: '/admin/users', role: 'Admin', page: () => <UsersPage /> }
];

function Redirect({ to }: { to: string }) {
  useEffect(() => { navigate(to, true); }, [to]);
  return null;
}

function App() {
  const path = usePath();
  const { user } = useAuth();

  if (path === '/login') return user ? <Redirect to={homeFor(user.role)} /> : <LoginPage />;
  if (path === '/register') return user ? <Redirect to={homeFor(user.role)} /> : <RegisterPage />;
  if (!user) return <Redirect to="/login" />;
  if (path === '/' || path === '') return <Redirect to={homeFor(user.role)} />;

  for (const r of ROUTES) {
    const params = match(r.path, path);
    if (!params) continue;
    if (r.role !== user.role) return <Redirect to={homeFor(user.role)} />;
    return <Shell>{r.page(params)}</Shell>;
  }
  return <Shell><Empty title="Page not found">Use the menu to find what you need.</Empty></Shell>;
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <ToastProvider>
        <App />
      </ToastProvider>
    </AuthProvider>
  </StrictMode>
);
