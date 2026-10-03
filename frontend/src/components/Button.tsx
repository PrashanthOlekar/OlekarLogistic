import type { ButtonHTMLAttributes } from 'react';

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'dark' | 'secondary' | 'ghost' | 'danger' | 'success';
  size?: 'sm' | 'lg';
  /** Full width. */
  block?: boolean;
  /** Shows a spinner and disables the button. */
  busy?: boolean;
};

export function Button({
  variant = 'primary',
  size,
  block,
  busy,
  children,
  className,
  disabled,
  type = 'button',
  ...rest
}: ButtonProps) {
  const classes = ['btn', `btn-${variant}`, size && `btn-${size}`, block && 'btn-block', className]
    .filter(Boolean)
    .join(' ');

  return (
    <button type={type} className={classes} disabled={disabled || busy} {...rest}>
      {busy && <span className="spin" aria-hidden="true" />}
      {children}
    </button>
  );
}
