import { useCallback, useState } from 'react';
import { ApiError } from '../lib/api';

/**
 * Runs a button action with a busy flag and a friendly error message.
 *
 *   const save = useAction();
 *   <Button busy={save.busy} onClick={() => save.run(() => api('/x', { body }))}>Save</Button>
 *   {save.error && <Alert kind="error">{save.error}</Alert>}
 */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = useCallback(async <T>(action: () => Promise<T>): Promise<T | undefined> => {
    setBusy(true);
    setError(null);
    try {
      return await action();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Something went wrong.');
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);

  return { busy, error, run };
}
