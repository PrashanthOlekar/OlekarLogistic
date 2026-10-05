import type { IconName } from '../components/Icon';
import type { Role } from '../types';

export interface NavItem {
  to: string;
  label: string;
  icon: IconName;
  /** Highlight only on this exact path (not its sub-pages). */
  exact?: boolean;
}

/** Sidebar menu for each role. */
export const NAVIGATION: Record<Role, NavItem[]> = {
  Customer: [
    { to: '/customer/book', label: 'Book a truck', icon: 'plus' },
    { to: '/customer/bookings', label: 'My bookings', icon: 'list' },
  ],
  Owner: [
    { to: '/owner', label: 'Loads & overview', icon: 'grid', exact: true },
    { to: '/owner/vehicles', label: 'Vehicles', icon: 'truck' },
    { to: '/owner/drivers', label: 'Drivers', icon: 'users' },
    { to: '/owner/trips', label: 'Trips', icon: 'route' },
    { to: '/owner/payouts', label: 'Payouts', icon: 'wallet' },
    { to: '/owner/documents', label: 'KYC documents', icon: 'file' },
  ],
  Driver: [
    { to: '/driver', label: 'My trips', icon: 'route', exact: true },
    { to: '/driver/documents', label: 'My documents', icon: 'file' },
  ],
  Admin: [
    { to: '/admin', label: 'Dashboard', icon: 'grid', exact: true },
    { to: '/admin/approvals', label: 'Approvals', icon: 'badge' },
    { to: '/admin/documents', label: 'Documents', icon: 'file' },
    { to: '/admin/bookings', label: 'Bookings', icon: 'list' },
    { to: '/admin/trips', label: 'Trips & POD', icon: 'route' },
    { to: '/admin/payments', label: 'Payments', icon: 'card' },
    { to: '/admin/settlements', label: 'Owner payouts', icon: 'wallet' },
    { to: '/admin/users', label: 'Users', icon: 'users' },
  ],
};

export const ROLE_TITLES: Record<Role, string> = {
  Customer: 'Customer',
  Owner: 'Lorry owner',
  Driver: 'Driver',
  Admin: 'Operations',
};

/** The first page each role sees after signing in. */
export const HOME_PAGES: Record<Role, string> = {
  Customer: '/customer/bookings',
  Owner: '/owner',
  Driver: '/driver',
  Admin: '/admin',
};

export function isActive(item: NavItem, path: string): boolean {
  if (item.exact) {
    return path === item.to;
  }
  return path === item.to || path.startsWith(`${item.to}/`);
}
