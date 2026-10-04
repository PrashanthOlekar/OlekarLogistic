/**
 * Sign-in and registration. Someone who arrives here already signed in is sent on to where they were
 * going, or their home page. Someone who signs in on these pages is sent on by the page itself
 * (registration goes to a different first page than sign-in), so this guard doesn't redirect them.
 */
import { useState } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { FullPageLoading } from '../components/Loading';
import { HOME_PAGES } from '../routes/navigation';
import { useAuth } from './useAuth';

export function PublicOnlyRoute() {
  const { status, user } = useAuth();
  const location = useLocation();
  const [signedInOnArrival] = useState(status !== 'anonymous');

  if (status === 'loading') {
    return <FullPageLoading />;
  }
  if (user && signedInOnArrival) {
    const from = (location.state as { from?: string } | null)?.from;
    return <Navigate to={from ?? HOME_PAGES[user.role]} replace />;
  }
  return <Outlet />;
}
