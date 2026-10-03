import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// In development the portal runs on http://localhost:5173
// and forwards every /api call to the ASP.NET Core API on http://localhost:5080.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
});
