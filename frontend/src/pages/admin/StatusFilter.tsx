/** A row of filter buttons above a table: [['', 'All'], ['Pending', 'Pending'], …] */
export function StatusFilter({
  options,
  value,
  onChange,
}: {
  options: [value: string, label: string][];
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="seg">
      {options.map(([optionValue, label]) => (
        <button
          key={optionValue || 'all'}
          aria-pressed={value === optionValue}
          onClick={() => onChange(optionValue)}
        >
          {label}
        </button>
      ))}
    </div>
  );
}

/** Adds ?status=… to a path when a filter is chosen. */
export function withStatus(path: string, status: string): string {
  return status ? `${path}?status=${status}` : path;
}
