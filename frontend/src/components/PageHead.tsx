import { createContext, useContext, type ReactNode } from 'react';

/** The small label above every page title (the signed-in role); set once by the app shell. */
export const PageEyebrowContext = createContext<string | null>(null);

interface PageHeadProps {
  title: string;
  sub?: ReactNode;
  /** Buttons or filters shown on the right. */
  children?: ReactNode;
}

/** The dark title band at the top of each page, in the style of the website's hero. */
export function PageHead({ title, sub, children }: PageHeadProps) {
  const eyebrow = useContext(PageEyebrowContext);
  return (
    <header className="page-head">
      <div className="page-head-text">
        {eyebrow && <span className="eyebrow">{eyebrow}</span>}
        <h1>{title}</h1>
        {sub && <p>{sub}</p>}
      </div>
      {children && <div className="page-head-actions">{children}</div>}
    </header>
  );
}
