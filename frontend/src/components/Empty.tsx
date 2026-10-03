import type { ReactNode } from 'react';

/** Shown when a list has nothing in it yet. */
export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <b>{title}</b>
      {children}
    </div>
  );
}

/** An empty state inside a card, for pages whose only content is a list. */
export function EmptyCard({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="card">
      <Empty title={title}>{children}</Empty>
    </div>
  );
}
