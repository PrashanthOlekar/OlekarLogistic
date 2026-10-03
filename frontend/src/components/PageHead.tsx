import type { ReactNode } from 'react';

interface PageHeadProps {
  title: string;
  sub?: ReactNode;
  /** Buttons or filters shown on the right. */
  children?: ReactNode;
}

export function PageHead({ title, sub, children }: PageHeadProps) {
  return (
    <header className="page-head">
      <div>
        <h1>{title}</h1>
        {sub && <p>{sub}</p>}
      </div>
      {children && <div className="row">{children}</div>}
    </header>
  );
}
