/** The ProCargo logo. "light" is for dark (navy) backgrounds. */
export function Logo({ variant = 'light' }: { variant?: 'light' | 'dark' }) {
  const file = variant === 'light' ? '/procargo-logo-white.svg' : '/procargo-logo.svg';
  return <img src={file} alt="ProCargo" width={150} height={34} />;
}
