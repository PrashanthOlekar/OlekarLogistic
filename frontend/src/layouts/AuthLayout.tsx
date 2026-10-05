/** Sign-in and registration: the website's navy highway hero, with the form in a white card. */
import type { ReactNode } from 'react';
import { Logo } from '../components';

const PROMISES = ['Verified owners & drivers', 'OTP at pickup & delivery', 'Live trip updates'];

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="auth">
      <div className="auth-glow" aria-hidden="true" />
      <div className="auth-inner">
        <section className="auth-art">
          <Logo />
          <div className="auth-copy">
            <span className="eyebrow">Moving India. Delivering Trust.</span>
            <h1>
              Move anything.
              <br />
              Anywhere.
              <br />
              <em>Across India.</em>
            </h1>
            <p>
              Book verified lorries, take loads for your trucks, or run today's trip. One sign-in for customers,
              lorry owners, drivers and the ProCargo team.
            </p>
            <ul className="auth-promises">
              {PROMISES.map((promise) => (
                <li key={promise}>{promise}</li>
              ))}
            </ul>
          </div>
        </section>

        <div className="auth-form">
          <div className="auth-card">{children}</div>
        </div>
      </div>
      <div className="road" aria-hidden="true" />
    </div>
  );
}
