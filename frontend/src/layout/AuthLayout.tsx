/** The two-column frame for the sign-in and registration pages. */
import type { ReactNode } from 'react';
import { Logo } from '../components';

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="auth">
      <aside className="auth-art">
        <Logo />
        <div className="stack">
          <h1>
            Move anything. <em>Anywhere.</em>
          </h1>
          <p>
            Book verified lorries, take loads for your trucks, or run today's trip. One sign-in for customers,
            lorry owners, drivers and the ProCargo team.
          </p>
        </div>
        <div className="road" aria-hidden="true" />
      </aside>

      <div className="auth-form">
        <div className="box">{children}</div>
      </div>
    </div>
  );
}
