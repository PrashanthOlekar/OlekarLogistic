/**
 * A very small router. The portal has about 20 pages and only needs paths with :params.
 *
 *   navigate('/customer/bookings/12');
 *   match('/customer/bookings/:id', '/customer/bookings/12')  →  { id: '12' }
 */
import { useEffect, useState, type AnchorHTMLAttributes, type MouseEvent } from 'react';

const listeners = new Set<() => void>();
window.addEventListener('popstate', notifyListeners);

function notifyListeners(): void {
  listeners.forEach((listener) => listener());
}

export function navigate(to: string, replace = false): void {
  if (replace) {
    history.replaceState({}, '', to);
  } else {
    history.pushState({}, '', to);
  }
  notifyListeners();
  window.scrollTo(0, 0);
}

/** The current path; the component re-renders when it changes. */
export function usePath(): string {
  const [path, setPath] = useState(location.pathname);

  useEffect(() => {
    const update = () => setPath(location.pathname);
    listeners.add(update);
    return () => {
      listeners.delete(update);
    };
  }, []);

  return path;
}

/** Returns the :params when `path` matches `pattern`, otherwise null. */
export function match(pattern: string, path: string): Record<string, string> | null {
  const patternParts = pattern.split('/').filter(Boolean);
  const pathParts = path.split('/').filter(Boolean);
  if (patternParts.length !== pathParts.length) {
    return null;
  }

  const params: Record<string, string> = {};
  for (let i = 0; i < patternParts.length; i++) {
    if (patternParts[i].startsWith(':')) {
      params[patternParts[i].slice(1)] = decodeURIComponent(pathParts[i]);
    } else if (patternParts[i] !== pathParts[i]) {
      return null;
    }
  }
  return params;
}

type LinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & { to: string };

/** An <a> that changes page without reloading. Ctrl/⌘-click still opens a new tab. */
export function Link({ to, onClick, ...rest }: LinkProps) {
  const handleClick = (event: MouseEvent<HTMLAnchorElement>) => {
    onClick?.(event);
    const opensNewTab = event.metaKey || event.ctrlKey || event.shiftKey || event.button !== 0;
    if (event.defaultPrevented || opensNewTab) {
      return;
    }
    event.preventDefault();
    navigate(to);
  };

  return <a href={to} onClick={handleClick} {...rest} />;
}
