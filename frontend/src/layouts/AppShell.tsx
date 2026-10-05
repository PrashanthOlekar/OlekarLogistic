/** The frame around every signed-in page: navy sidebar, mobile top bar and content area. */
import { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router';
import { useAuth } from '../auth';
import { Icon, Logo } from '../components';
import { PageEyebrowContext } from '../components/PageHead';
import { NAVIGATION, ROLE_TITLES, isActive } from '../routes/navigation';

/** Used as a layout route: the matched page renders in <Outlet />. */
export function AppShell() {
  const { user, signOut } = useAuth();
  const { pathname: path } = useLocation();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);

  if (!user) {
    return null;
  }

  const items = NAVIGATION[user.role];

  const handleSignOut = async () => {
    await signOut();
    navigate('/login', { replace: true });
  };

  return (
    <div className="shell">
      <div className="topbar">
        <button onClick={() => setMenuOpen(true)} aria-label="Open menu">
          <Icon name="menu" />
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
            <Icon name={item.icon} />
            {item.label}
          </Link>
        ))}

        <div className="side-foot">
          <div className="side-user">
            <b>{user.fullName}</b>
            <span className="mono">{user.mobile}</span>
          </div>
          <button onClick={handleSignOut}>
            <Icon name="logout" size={16} />
            Sign out
          </button>
        </div>
      </nav>

      <main className="main">
        <PageEyebrowContext.Provider value={ROLE_TITLES[user.role]}>
          <Outlet />
        </PageEyebrowContext.Provider>
      </main>
    </div>
  );
}
