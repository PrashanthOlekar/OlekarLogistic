import { useEffect } from 'react';
import { useLocation } from 'react-router';

/** Starts each new page at the top, as the previous portal did. */
export function ScrollToTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}
