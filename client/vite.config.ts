import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// In development the app runs on port 5173 and forwards /api to the .NET server on 5270.
// The browser sees one origin, so the session cookie and the CSRF header work as in production.
// A build goes into the server's wwwroot, which the API serves in production.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: { '/api': 'http://localhost:5270' },
  },
  build: {
    outDir: '../IronMindGym/wwwroot',
    emptyOutDir: true,
    // Libraries change less often than the app, so browsers keep them cached across releases.
    rolldownOptions: { output: { codeSplitting: { groups: [{ name: 'vendor', test: /node_modules/ }] } } },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    testTimeout: 20000,
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/main.tsx'],
      reporter: ['text-summary', 'text', 'cobertura'],
      thresholds: { lines: 80, statements: 80, functions: 80, branches: 80 },
    },
  },
});
