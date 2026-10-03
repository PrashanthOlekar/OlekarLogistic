/** Formatting for money, weights and dates, Indian style. */

/** ₹1,23,456 or ₹816.40 */
export function inr(amount?: number | null): string {
  if (amount == null) {
    return '—';
  }
  const isWhole = Number.isInteger(Math.round(Number(amount) * 100) / 100);
  const digits = isWhole ? 0 : 2;
  return (
    '₹' +
    Number(amount).toLocaleString('en-IN', { minimumFractionDigits: digits, maximumFractionDigits: digits })
  );
}

/** 3,500 kg */
export function kg(weight?: number | null): string {
  return weight == null ? '—' : `${Number(weight).toLocaleString('en-IN')} kg`;
}

/** 03 Oct 2026. Accepts "2026-10-03" or a full timestamp. */
export function date(value?: string | null): string {
  if (!value) {
    return '—';
  }
  const parsed = new Date(value.length === 10 ? `${value}T00:00:00` : value);
  return parsed.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
}

/** 03 Oct, 02:30 pm. The API sends UTC times. */
export function dateTime(value?: string | null): string {
  if (!value) {
    return '—';
  }
  const isZoned = value.endsWith('Z') || value.includes('+');
  const parsed = new Date(isZoned ? value : `${value}Z`);
  return parsed.toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/** Short weekday, e.g. "Sat". */
export function weekday(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00`).toLocaleDateString('en-IN', { weekday: 'short' });
}

/** Today's date in India as "YYYY-MM-DD", for date inputs. */
export function today(): string {
  const indiaNow = new Date(Date.now() + 5.5 * 60 * 60 * 1000);
  return indiaNow.toISOString().slice(0, 10);
}

/** "1 day" / "2 days" */
export function plural(count: number, word: string): string {
  return `${count} ${word}${count === 1 ? '' : 's'}`;
}
