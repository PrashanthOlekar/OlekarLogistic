/** Every signed-in page, the role allowed to open it, and what it renders. */
import type { ReactNode } from 'react';
import type { Role } from './lib/types';
import { AdminBookingsPage } from './pages/admin/AdminBookingsPage';
import { AdminDocumentsPage } from './pages/admin/AdminDocumentsPage';
import { AdminHomePage } from './pages/admin/AdminHomePage';
import { AdminTripsPage } from './pages/admin/AdminTripsPage';
import { ApprovalsPage } from './pages/admin/ApprovalsPage';
import { PaymentsPage } from './pages/admin/PaymentsPage';
import { SettlementsPage } from './pages/admin/SettlementsPage';
import { UsersPage } from './pages/admin/UsersPage';
import { BookingDetailPage } from './pages/customer/BookingDetailPage';
import { BookingsPage } from './pages/customer/BookingsPage';
import { NewBookingPage } from './pages/customer/NewBookingPage';
import { DriverHomePage } from './pages/driver/DriverHomePage';
import { DriversPage } from './pages/owner/DriversPage';
import { OwnerHomePage } from './pages/owner/OwnerHomePage';
import { OwnerTripsPage } from './pages/owner/OwnerTripsPage';
import { PayoutsPage } from './pages/owner/PayoutsPage';
import { VehiclesPage } from './pages/owner/VehiclesPage';
import { MyDocumentsPage } from './pages/shared/MyDocumentsPage';

export interface AppRoute {
  path: string;
  role: Role;
  render: (params: Record<string, string>) => ReactNode;
}

export const ROUTES: AppRoute[] = [
  // Customers
  { path: '/customer/book', role: 'Customer', render: () => <NewBookingPage /> },
  { path: '/customer/bookings', role: 'Customer', render: () => <BookingsPage /> },
  {
    path: '/customer/bookings/:id',
    role: 'Customer',
    render: (params) => <BookingDetailPage id={params.id} />,
  },

  // Lorry owners
  { path: '/owner', role: 'Owner', render: () => <OwnerHomePage /> },
  { path: '/owner/vehicles', role: 'Owner', render: () => <VehiclesPage /> },
  { path: '/owner/drivers', role: 'Owner', render: () => <DriversPage /> },
  { path: '/owner/trips', role: 'Owner', render: () => <OwnerTripsPage /> },
  { path: '/owner/payouts', role: 'Owner', render: () => <PayoutsPage /> },
  { path: '/owner/documents', role: 'Owner', render: () => <MyDocumentsPage /> },

  // Drivers
  { path: '/driver', role: 'Driver', render: () => <DriverHomePage /> },
  { path: '/driver/documents', role: 'Driver', render: () => <MyDocumentsPage /> },

  // Operations team
  { path: '/admin', role: 'Admin', render: () => <AdminHomePage /> },
  { path: '/admin/approvals', role: 'Admin', render: () => <ApprovalsPage /> },
  { path: '/admin/documents', role: 'Admin', render: () => <AdminDocumentsPage /> },
  { path: '/admin/bookings', role: 'Admin', render: () => <AdminBookingsPage /> },
  { path: '/admin/trips', role: 'Admin', render: () => <AdminTripsPage /> },
  { path: '/admin/payments', role: 'Admin', render: () => <PaymentsPage /> },
  { path: '/admin/settlements', role: 'Admin', render: () => <SettlementsPage /> },
  { path: '/admin/users', role: 'Admin', render: () => <UsersPage /> },
];
