/**
 * Only lets the given roles through; anyone else goes to their own home page.
 * This only decides what the portal shows: the API enforces the same rules on every request.
 */
import { Navigate, Outlet } from 'react-router';
import { HOME_PAGES } from '../routes/navigation';
import type { Role } from '../types';
import { useAuth } from './useAuth';

export function RoleProtectedRoute({ roles }: { roles: Role[] }) {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }
  if (!roles.includes(user.role)) {
    return <Navigate to={HOME_PAGES[user.role]} replace />;
  }
  return <Outlet />;
}
