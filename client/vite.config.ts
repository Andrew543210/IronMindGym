import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// In development /api goes to the .NET server on 5270, so the browser sees one origin, as in production.
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
    // Without this a stylesheet imported in a test is empty, so the stylesheet tests would read nothing.
    css: { include: [/src\/styles\/.+\.css/] },
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/main.tsx'],
      reporter: ['text-summary', 'text', 'cobertura'],
      // Each folder keeps the bar on its own, so a well-tested folder cannot hide one that is not.
      thresholds: {
        lines: 80, statements: 80, functions: 80, branches: 80,
        'src/*.{ts,tsx}': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/api/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/components/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/i18n/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/*.{ts,tsx}': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/auth/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/friends/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/profile/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/progress/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/workouts/*.{ts,tsx}': { lines: 80, statements: 80, functions: 80, branches: 80 },
        'src/pages/workouts/logger/**': { lines: 80, statements: 80, functions: 80, branches: 80 },
      },
    },
  },
});
