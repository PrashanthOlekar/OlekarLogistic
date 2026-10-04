import { useContext } from 'react';
import { AuthContext, type AuthState } from './AuthContext';

export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return state;
}

/** For pages that only render for a signed-in user (inside <ProtectedRoute>). */
export function useSignedInUser() {
  const { user } = useAuth();
  if (!user) {
    throw new Error('useSignedInUser must be used inside <ProtectedRoute>.');
  }
  return user;
}
