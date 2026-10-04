/**
 * The API client and the AuthProvider talk through these events, so the client
 * doesn't need React and the provider doesn't need to know about HTTP.
 */
import type { UserProfile } from '../types';

export type SessionEndReason = 'signed-out' | 'expired' | 'account-inactive';

export type SessionEvent =
  | { type: 'refreshed'; user: UserProfile }
  | { type: 'ended'; reason: SessionEndReason };

type Listener = (event: SessionEvent) => void;

const listeners = new Set<Listener>();

export function onSessionEvent(listener: Listener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function emitSessionEvent(event: SessionEvent): void {
  listeners.forEach((listener) => listener(event));
}
