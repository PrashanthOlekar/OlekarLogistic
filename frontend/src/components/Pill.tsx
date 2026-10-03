import { describeStatus } from '../lib/status';

/** A coloured status label, e.g. <Pill status="InTransit" /> → "In transit". */
export function Pill({ status, label }: { status: string; label?: string }) {
  const { label: defaultLabel, colour } = describeStatus(status);
  return <span className={`pill ${colour}`}>{label ?? defaultLabel}</span>;
}
