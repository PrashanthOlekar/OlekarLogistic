import type { Role } from '../lib/types';

export interface NavItem {
  to: string;
  label: string;
  /** Highlight only on this exact path (not its sub-pages). */
  exact?: boolean;
}

/** Sidebar menu for each role. */
export const NAVIGATION: Record<Role, NavItem[]> = {
  Customer: [
    { to: '/customer/book', label: 'Book a truck' },
    { to: '/customer/bookings', label: 'My bookings' },
  ],
  Owner: [
    { to: '/owner', label: 'Loads & overview', exact: true },
    { to: '/owner/vehicles', label: 'Vehicles' },
    { to: '/owner/drivers', label: 'Drivers' },
    { to: '/owner/trips', label: 'Trips' },
    { to: '/owner/payouts', label: 'Payouts' },
    { to: '/owner/documents', label: 'KYC documents' },
  ],
  Driver: [
    { to: '/driver', label: 'My trips', exact: true },
    { to: '/driver/documents', label: 'My documents' },
  ],
  Admin: [
    { to: '/admin', label: 'Dashboard', exact: true },
    { to: '/admin/approvals', label: 'Approvals' },
    { to: '/admin/documents', label: 'Documents' },
    { to: '/admin/bookings', label: 'Bookings' },
    { to: '/admin/trips', label: 'Trips & POD' },
    { to: '/admin/payments', label: 'Payments' },
    { to: '/admin/settlements', label: 'Owner payouts' },
    { to: '/admin/users', label: 'Users' },
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
