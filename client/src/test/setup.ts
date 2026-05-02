import '@testing-library/jest-dom/vitest';
import { cleanup, configure } from '@testing-library/react';
import { afterEach, vi } from 'vitest';
import { forgetCsrfToken } from '../api';
import { setLanguage } from '../i18n/language';

// Pages render many fields; under a parallel run the default one second is too short.
configure({ asyncUtilTimeout: 5000 });

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  forgetCsrfToken();
  setLanguage('en');
  localStorage.clear();
});
