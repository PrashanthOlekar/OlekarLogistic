import type { ReactNode } from 'react';

/** A price breakdown: label on the left, amount on the right, total last. */
export function MoneyRows({ children }: { children: ReactNode }) {
  return <div className="money-rows">{children}</div>;
}

export function MoneyRow({ label, value, total }: { label: ReactNode; value: ReactNode; total?: boolean }) {
  return (
    <div className={total ? 'total' : undefined}>
      <span>{label}</span>
      {total ? <b>{value}</b> : <span>{value}</span>}
    </div>
  );
}
