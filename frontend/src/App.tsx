/** Providers and the router. Pages and their access rules are in routes/AppRoutes.tsx. */
import { BrowserRouter } from 'react-router';
import { AuthProvider } from './auth';
import { ToastProvider } from './components';
import { ScrollToTop } from './routes/ScrollToTop';
import { AppRoutes } from './routes/AppRoutes';

export function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <BrowserRouter>
          <ScrollToTop />
          <AppRoutes />
        </BrowserRouter>
      </ToastProvider>
    </AuthProvider>
  );
}
