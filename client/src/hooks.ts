import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from './api/client';
import { useI18n } from './i18n/i18n';

interface Loaded<T> {
  key: string;
  url: string;
  data?: T;
  error?: unknown;
}

export interface ApiOptions {
  /** Keeps the answer of the previous url on screen while the next one loads, for a list filtered or paged in place. */
  keepPrevious?: boolean;
}

export interface Loading<T> {
  data: T | undefined;
  error: unknown;
  loading: boolean;
  /** The data shown belongs to an earlier request: the url changed with `keepPrevious`, or a reload is running. */
  isStale: boolean;
  reload: () => void;
}

/**
 * Loads JSON for a page; a null url waits. A new url starts without data, so a form never shows or changes
 * the record of another address, unless `keepPrevious` asks to keep the last answer. `reload` reads the same
 * url again and keeps its data meanwhile. Names such as exercises come translated from the API, so a new
 * interface language loads them again. A request the page no longer needs is aborted.
 */
export function useApi<T>(url: string | null, { keepPrevious = false }: ApiOptions = {}): Loading<T> {
  const [version, setVersion] = useState(0);
  const [result, setResult] = useState<Loaded<T>>();
  const { language } = useI18n();
  const key = `${version}:${language}:${url}`;

  useEffect(() => {
    if (url === null) return;
    const controller = new AbortController();
    api.get<T>(url, { signal: controller.signal }).then(
      (data) => controller.signal.aborted || setResult({ key, url, data }),
      (error: unknown) => controller.signal.aborted || setResult({ key, url, error }),
    );
    return () => controller.abort();
  }, [url, key]);

  const reload = useCallback(() => setVersion((current) => current + 1), []);
  if (url === null) return { data: undefined, error: undefined, loading: false, isStale: false, reload };
  const current = result?.key === key ? result : undefined;
  if (current) return { data: current.data, error: current.error, loading: false, isStale: false, reload };
  const earlier = result && (keepPrevious || result.url === url) ? result.data : undefined;
  return { data: earlier, error: undefined, loading: true, isStale: earlier !== undefined, reload };
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

/**
 * The current time, refreshed every interval and as soon as a hidden tab is shown again.
 * Only the components that show a clock use it, so the rest of a page does not render on every tick.
 */
export function useNow(intervalMs: number): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const tick = () => setNow(Date.now());
    const timer = window.setInterval(tick, intervalMs);
    const onVisible = () => {
      if (!document.hidden) tick();
    };
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, [intervalMs]);
  return now;
}
