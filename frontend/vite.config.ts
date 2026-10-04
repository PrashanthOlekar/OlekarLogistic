import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// The portal runs on http://localhost:5173 and calls the API at VITE_API_BASE_URL (see .env.development).
// The /api proxy below is only used when VITE_API_BASE_URL is the relative "/api/v1".
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'https://localhost:7001', changeOrigin: true, secure: false },
    },
  },
});
