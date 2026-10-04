import { Alert } from './Alert';

interface LoadingProps {
  state: { loading: boolean; error: string | null; data: unknown };
}

/** Shows a spinner while the first load runs, or the error if it failed. Otherwise nothing. */
export function Loading({ state }: LoadingProps) {
  if (state.error) {
    return (
      <Alert kind="error" title="Couldn't load this page">
        {state.error}
      </Alert>
    );
  }

  if (state.loading && !state.data) {
    return (
      <div className="empty">
        <span className="spin" /> Loading…
      </div>
    );
  }

  return null;
}

/** Shown while a saved sign-in is being renewed, before any page can render. */
export function FullPageLoading() {
  return (
    <div className="empty full-page" role="status">
      <span className="spin" /> Loading…
    </div>
  );
}
