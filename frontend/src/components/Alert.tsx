import type { ReactNode } from 'react';

interface AlertProps {
  kind?: 'info' | 'warn' | 'error' | 'success';
  title?: string;
  children?: ReactNode;
  className?: string;
}

export function Alert({ kind = 'info', title, children, className }: AlertProps) {
  return (
    <div
      className={['alert', kind, className].filter(Boolean).join(' ')}
      role={kind === 'error' ? 'alert' : undefined}
    >
      <div>
        {title && <b>{title}</b>}
        {children}
      </div>
    </div>
  );
}
