/** "Bengaluru → Hubballi" */
export function RouteLabel({ from, to }: { from?: string | null; to?: string | null }) {
  return (
    <span className="route">
      {from ?? '—'}{' '}
      <span className="arrow" role="img" aria-label="to">
        →
      </span>{' '}
      {to ?? '—'}
    </span>
  );
}
