/** Line icons for the menu and headers, drawn on a 24 × 24 grid with a 2 px stroke. */
export type IconName =
  | 'plus'
  | 'list'
  | 'grid'
  | 'truck'
  | 'users'
  | 'route'
  | 'wallet'
  | 'file'
  | 'badge'
  | 'card'
  | 'logout'
  | 'menu';

const PATHS: Record<IconName, string> = {
  plus: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Z M12 8v8 M8 12h8',
  list: 'M9 6h11 M9 12h11 M9 18h11 M4 6h.01 M4 12h.01 M4 18h.01',
  grid: 'M4 4h6v6H4Z M14 4h6v6h-6Z M4 14h6v6H4Z M14 14h6v6h-6Z',
  truck: 'M2 6h12v10H2Z M14 9h4l4 4v3h-8 M6 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z M17 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z',
  users:
    'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z M2 21c0-3.9 3.1-7 7-7s7 3.1 7 7 M17 3.5a3.5 3.5 0 0 1 0 7 M19 14.3c1.8.8 3 2.6 3 4.7',
  route: 'M6 21a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z M18 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z M9 18h6.5a3.5 3.5 0 0 0 0-7h-7a3.5 3.5 0 0 1 0-7H15',
  wallet: 'M4 6h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4Z M4 6V5a1 1 0 0 1 1-1h11 M16 13h.01',
  file: 'M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8Z M14 3v5h5 M9 14l2 2 4-4',
  badge: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Z M8 12l3 3 5-6',
  card: 'M3 6h18v12H3Z M3 10h18 M7 15h4',
  logout: 'M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9',
  menu: 'M4 6h16 M4 12h16 M4 18h16',
};

export function Icon({ name, size = 20 }: { name: IconName; size?: number }) {
  return (
    <svg
      className="icon"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={PATHS[name]} />
    </svg>
  );
}
