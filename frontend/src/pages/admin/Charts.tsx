/**
 * Simple dashboard charts drawn with SVG, so bar sizes are attributes rather than inline styles.
 */
import { Empty } from '../../components';
import { weekday } from '../../lib/format';

export interface BarRow {
  label: string;
  count: number;
}

/** Horizontal bars, longest first. */
export function BarList({ rows }: { rows: BarRow[] }) {
  if (rows.length === 0) {
    return <Empty title="No data yet" />;
  }

  const largest = Math.max(1, ...rows.map((row) => row.count));

  return (
    <div className="bars">
      {rows.map((row) => (
        <div className="bar" key={row.label}>
          <span>{row.label}</span>
          <svg viewBox="0 0 100 10" preserveAspectRatio="none" aria-hidden="true">
            <rect className="track" width="100" height="10" rx="3" />
            <rect className="fill" width={(row.count / largest) * 100} height="10" rx="3" />
          </svg>
          <b>{row.count}</b>
        </div>
      ))}
    </div>
  );
}

const COLUMN_HEIGHT = 120;

/** Bookings per day for the last seven days; today is highlighted. */
export function DailyColumns({ days }: { days: { date: string; count: number }[] }) {
  const largest = Math.max(1, ...days.map((day) => day.count));

  return (
    <div className="columns">
      {days.map((day) => {
        const height = (day.count / largest) * (COLUMN_HEIGHT - 3) + 3;
        return (
          <div key={day.date}>
            <b>{day.count}</b>
            <svg viewBox={`0 0 44 ${COLUMN_HEIGHT}`} preserveAspectRatio="none" aria-hidden="true">
              <rect className="fill" x="0" y={COLUMN_HEIGHT - height} width="44" height={height} rx="5" />
            </svg>
            <span>{weekday(day.date)}</span>
          </div>
        );
      })}
    </div>
  );
}
