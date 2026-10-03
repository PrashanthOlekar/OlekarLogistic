/** The frame around every signed-in page: navy sidebar, mobile top bar and content area. */
import { useState, type ReactNode } from 'react';
import { Logo } from '../components';
import { Link, navigate, usePath } from '../lib/router';
import { useAuth } from '../state/AuthContext';
import { NAVIGATION, ROLE_TITLES, isActive } from './navigation';

export function AppShell({ children }: { children: ReactNode }) {
  const { user, signOut } = useAuth();
  const path = usePath();
  const [menuOpen, setMenuOpen] = useState(false);

  if (!user) {
    return null;
  }

  const items = NAVIGATION[user.role];

  const handleSignOut = () => {
    signOut();
    navigate('/login', true);
  };

  return (
    <div className="shell">
      <div className="topbar">
        <button onClick={() => setMenuOpen(true)} aria-label="Open menu">
          ☰
        </button>
        <Logo />
      </div>

      <nav className={menuOpen ? 'side open' : 'side'} aria-label="Main" onClick={() => setMenuOpen(false)}>
        <Link to={items[0].to} className="brand">
          <Logo />
          <small>Moving India. Delivering Trust.</small>
        </Link>

        <div className="role">{ROLE_TITLES[user.role]}</div>

        {items.map((item) => (
          <Link
            key={item.to}
            to={item.to}
            className="navlink"
            aria-current={isActive(item, path) ? 'page' : undefined}
          >
            {item.label}
          </Link>
        ))}

        <div className="side-foot">
          <div className="side-user">
            <b>{user.fullName}</b>
            <span className="mono">{user.mobile}</span>
          </div>
          <button onClick={handleSignOut}>Sign out</button>
        </div>
      </nav>

      <main className="main">{children}</main>
    </div>
  );
}
