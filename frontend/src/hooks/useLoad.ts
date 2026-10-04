import { useCallback, useEffect, useRef, useState } from 'react';
import { toApiError } from '../api/errors';

export interface LoadState<T> {
  data: T | null;
  error: string | null;
  loading: boolean;
  reload: () => Promise<void>;
}

/**
 * Loads data when the component appears and whenever `deps` change.
 * Answers that arrive after a newer request has started are ignored.
 *
 *   const booking = useLoad(() => bookingsApi.get(id), [id]);
 *   booking.data, booking.loading, booking.error, booking.reload()
 */
export function useLoad<T>(load: () => Promise<T>, deps: unknown[] = []): LoadState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const latestRequest = useRef(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const reload = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);
    try {
      const result = await load();
      if (request === latestRequest.current) {
        setData(result);
        setError(null);
      }
    } catch (caught) {
      if (request === latestRequest.current) {
        setError(toApiError(caught).message);
      }
    } finally {
      if (request === latestRequest.current) {
        setLoading(false);
      }
    }
  }, deps);

  useEffect(() => {
    reload();
  }, [reload]);

  return { data, error, loading, reload };
}
