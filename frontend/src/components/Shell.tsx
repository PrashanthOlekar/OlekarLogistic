import { useState, type ReactNode } from 'react';
import type { Role } from '../api';
import { Link, navigate, usePath } from '../router';
import { useAuth } from '../state';

type NavItem = { to: string; label: string; exact?: boolean };
export const NAV: Record<Role, NavItem[]> = {
  Customer: [
    { to: '/customer/book', label: 'Book a truck' },
    { to: '/customer/bookings', label: 'My bookings' }
  ],
  Owner: [
    { to: '/owner', label: 'Loads & overview', exact: true },
    { to: '/owner/vehicles', label: 'Vehicles' },
    { to: '/owner/drivers', label: 'Drivers' },
    { to: '/owner/trips', label: 'Trips' },
    { to: '/owner/payouts', label: 'Payouts' },
    { to: '/owner/documents', label: 'KYC documents' }
  ],
  Driver: [
    { to: '/driver', label: 'My trips', exact: true },
    { to: '/driver/documents', label: 'My documents' }
  ],
  Admin: [
    { to: '/admin', label: 'Dashboard', exact: true },
    { to: '/admin/approvals', label: 'Approvals' },
    { to: '/admin/documents', label: 'Documents' },
    { to: '/admin/bookings', label: 'Bookings' },
    { to: '/admin/trips', label: 'Trips & POD' },
    { to: '/admin/payments', label: 'Payments' },
    { to: '/admin/settlements', label: 'Owner payouts' },
    { to: '/admin/users', label: 'Users' }
  ]
};
const ROLE_NAME: Record<Role, string> = { Customer: 'Customer', Owner: 'Lorry owner', Driver: 'Driver', Admin: 'Operations' };

export function Shell({ children }: { children: ReactNode }) {
  const { user, signOut } = useAuth();
  const path = usePath();
  const [open, setOpen] = useState(false);
  if (!user) return null;
  const items = NAV[user.role];
  const active = (i: NavItem) => (i.exact ? path === i.to : path === i.to || path.startsWith(i.to + '/'));
  return (
    <div className="shell">
      <div className="topbar">
        <button onClick={() => setOpen(true)} aria-label="Open menu">☰</button>
        <b style={{ fontFamily: 'var(--display)' }}>Olekar Logistics</b>
      </div>
      <nav className={`side ${open ? 'open' : ''}`} aria-label="Main" onClick={() => setOpen(false)}>
        <Link to={items[0].to} className="brand">
          <span className="brand-mark" aria-hidden="true">O</span>
          <span><b>Olekar Logistics</b><small>Moving India. Delivering Trust.</small></span>
        </Link>
        <div className="role">{ROLE_NAME[user.role]}</div>
        {items.map((i) => (
          <Link key={i.to} to={i.to} className="navlink" aria-current={active(i) ? 'page' : undefined}>{i.label}</Link>
        ))}
        <div className="side-foot">
          <div><b style={{ color: '#fff' }}>{user.fullName}</b><div className="mono" style={{ color: '#9fb0d4' }}>{user.mobile}</div></div>
          <button onClick={() => { signOut(); navigate('/login', true); }}>Sign out</button>
        </div>
      </nav>
      <main className="main">{children}</main>
    </div>
  );
}
