import type { ReactNode } from 'react';

interface FieldProps {
  label: string;
  hint?: ReactNode;
  error?: string | null;
  children: ReactNode;
}

/** A labelled form control with an optional hint or error underneath. */
export function Field({ label, hint, error, children }: FieldProps) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
      {error ? <em className="err">{error}</em> : hint ? <em className="hint">{hint}</em> : null}
    </label>
  );
}
