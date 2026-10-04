/** Lets signed-in users through; sends everyone else to the sign-in page and back afterwards. */
import { Navigate, Outlet, useLocation } from 'react-router';
import { FullPageLoading } from '../components/Loading';
import { useAuth } from './useAuth';

export function ProtectedRoute() {
  const { status, user } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullPageLoading />;
  }
  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }
  return <Outlet />;
}
