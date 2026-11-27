import '@testing-library/jest-dom/vitest';
import { cleanup, configure } from '@testing-library/react';
import { afterEach, vi } from 'vitest';
import { forgetCsrfToken } from '../api';
import { setLanguage } from '../i18n/language';
import { unmocked } from './harness';

// Pages render many fields; under a parallel run the default one second is too short.
configure({ asyncUtilTimeout: 5000 });

afterEach((context) => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  forgetCsrfToken();
  setLanguage('en');
  localStorage.clear();
  // A request no test route answers is a mistake in the test, not a 404 the page should handle.
  const missed = unmocked.splice(0);
  if (missed.length > 0) throw new Error(`Requests without a mocked answer in "${context.task.name}": ${missed.join(', ')}`);
});
