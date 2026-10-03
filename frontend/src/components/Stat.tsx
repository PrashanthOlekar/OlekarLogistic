import type { ReactNode } from 'react';

interface StatProps {
  label: string;
  value: ReactNode;
  note?: ReactNode;
  /** Highlights the number in orange when something needs attention. */
  alert?: boolean;
}

export function Stat({ label, value, note, alert }: StatProps) {
  return (
    <div className={alert ? 'card stat alert' : 'card stat'}>
      <small>{label}</small>
      <b>{value}</b>
      {note && <span>{note}</span>}
    </div>
  );
}
