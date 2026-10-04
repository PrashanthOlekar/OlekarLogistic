/** Sign-in and registration: a signed-in user is sent on to where they were going, or their home page. */
import { Navigate, Outlet, useLocation } from 'react-router';
import { FullPageLoading } from '../components/Loading';
import { HOME_PAGES } from '../routes/navigation';
import { useAuth } from './useAuth';

export function PublicOnlyRoute() {
  const { status, user } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullPageLoading />;
  }
  if (user) {
    const from = (location.state as { from?: string } | null)?.from;
    return <Navigate to={from ?? HOME_PAGES[user.role]} replace />;
  }
  return <Outlet />;
}
