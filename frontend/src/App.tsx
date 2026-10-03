/** Decides which page to show for the current address and who is signed in. */
import { useEffect } from 'react';
import { EmptyCard } from './components';
import { AppShell } from './layout/AppShell';
import { HOME_PAGES } from './layout/navigation';
import { match, navigate, usePath } from './lib/router';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { ROUTES } from './routes';
import { useAuth } from './state/AuthContext';

export function App() {
  const path = usePath();
  const { user } = useAuth();

  // Public pages
  if (path === '/login' || path === '/register') {
    if (user) {
      return <Redirect to={HOME_PAGES[user.role]} />;
    }
    return path === '/login' ? <LoginPage /> : <RegisterPage />;
  }

  // Everything else needs a signed-in user
  if (!user) {
    return <Redirect to="/login" />;
  }
  if (path === '/' || path === '') {
    return <Redirect to={HOME_PAGES[user.role]} />;
  }

  for (const route of ROUTES) {
    const params = match(route.path, path);
    if (!params) {
      continue;
    }
    if (route.role !== user.role) {
      return <Redirect to={HOME_PAGES[user.role]} />;
    }
    return <AppShell>{route.render(params)}</AppShell>;
  }

  return (
    <AppShell>
      <EmptyCard title="Page not found">Use the menu to find what you need.</EmptyCard>
    </AppShell>
  );
}

function Redirect({ to }: { to: string }) {
  useEffect(() => {
    navigate(to, true);
  }, [to]);
  return null;
}
