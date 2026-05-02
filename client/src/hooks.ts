import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from './api';
import { useI18n } from './i18n/i18n';

interface Loaded<T> {
  key: string;
  data?: T;
  error?: unknown;
}

/**
 * Loads JSON for a page. While the url changes the previous data is kept until the new answer arrives,
 * so lists do not flash empty; `reload` reads the same url again after a change.
 * Names such as exercises come translated from the API, so a new interface language loads them again.
 */
export function useApi<T>(url: string) {
  const [version, setVersion] = useState(0);
  const [result, setResult] = useState<Loaded<T>>();
  const { language } = useI18n();
  const key = `${version}:${language}:${url}`;

  useEffect(() => {
    let active = true;
    api.get<T>(url).then(
      (data) => active && setResult({ key, data }),
      (error: unknown) => active && setResult({ key, error }),
    );
    return () => {
      active = false;
    };
  }, [url, key]);

  const reload = useCallback(() => setVersion((current) => current + 1), []);
  const current = result?.key === key ? result : undefined;
  return {
    data: current ? current.data : result?.data,
    error: current?.error,
    loading: current === undefined,
    reload,
  };
}

/**
 * Runs one change at a time and remembers its error. A second tap while the first request is running
 * is ignored, which keeps a double tap from saving twice.
 */
export function useAction() {
  const running = useRef(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();

  const run = useCallback(async (action: () => Promise<void>) => {
    if (running.current) return;
    running.current = true;
    setBusy(true);
    setError(undefined);
    try {
      await action();
    } catch (failure) {
      setError(failure);
    } finally {
      running.current = false;
      setBusy(false);
    }
  }, []);

  return { busy, error, run, setError };
}

export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = `${title} · IronMind`;
  }, [title]);
}
