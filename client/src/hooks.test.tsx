import { act, renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useApi } from './hooks';
import { setLanguage } from './i18n/language';
import { mockApi, respond } from './test/harness';

/** A reply that waits until the test lets it through. */
function held<T>(body: T) {
  let release = () => {};
  const reply = new Promise<T>((resolve) => {
    release = () => resolve(body);
  });
  return { reply: () => reply, release: () => release() };
}

describe('useApi', () => {
  it('starts a new url without the data of the previous one', async () => {
    const second = held({ name: 'second' });
    mockApi({ 'GET /api/first': { name: 'first' }, 'GET /api/second': second.reply });
    const { result, rerender } = renderHook(({ url }) => useApi<{ name: string }>(url), { initialProps: { url: '/api/first' } });
    await waitFor(() => expect(result.current.data).toEqual({ name: 'first' }));

    rerender({ url: '/api/second' });
    expect(result.current).toMatchObject({ data: undefined, loading: true, isStale: false });

    await act(async () => second.release());
    await waitFor(() => expect(result.current.data).toEqual({ name: 'second' }));
  });

  it('keeps the previous answer on request and marks it as stale', async () => {
    const second = held({ page: 2 });
    mockApi({ 'GET /api/list?page=1': { page: 1 }, 'GET /api/list?page=2': second.reply });
    const { result, rerender } = renderHook(({ url }) => useApi<{ page: number }>(url, { keepPrevious: true }),
      { initialProps: { url: '/api/list?page=1' } });
    await waitFor(() => expect(result.current.data).toEqual({ page: 1 }));

    rerender({ url: '/api/list?page=2' });
    expect(result.current).toMatchObject({ data: { page: 1 }, loading: true, isStale: true });

    await act(async () => second.release());
    await waitFor(() => expect(result.current).toMatchObject({ data: { page: 2 }, isStale: false }));
  });

  it('keeps the data of the same url during a reload and after a new language', async () => {
    let version = 0;
    const calls = mockApi({ 'GET /api/goals': () => ({ version: ++version }) });
    const { result } = renderHook(() => useApi<{ version: number }>('/api/goals'));
    await waitFor(() => expect(result.current.data).toEqual({ version: 1 }));

    act(() => result.current.reload());
    expect(result.current).toMatchObject({ data: { version: 1 }, loading: true, isStale: true });
    await waitFor(() => expect(result.current.data).toEqual({ version: 2 }));

    act(() => setLanguage('uk'));
    await waitFor(() => expect(result.current.data).toEqual({ version: 3 }));
    expect(calls.at(-1)?.headers['Accept-Language']).toBe('uk');
  });

  it('waits for a url and aborts a request that is no longer needed', async () => {
    const signals: AbortSignal[] = [];
    mockApi({});
    const stubbed = globalThis.fetch;
    globalThis.fetch = (input, init) => {
      if (init?.signal) signals.push(init.signal);
      return init?.signal?.aborted ? Promise.reject(new DOMException('Aborted', 'AbortError')) : stubbed(input, init);
    };
    const { result, rerender, unmount } = renderHook(({ url }) => useApi(url), { initialProps: { url: null as string | null } });
    expect(result.current).toMatchObject({ data: undefined, loading: false });
    expect(signals).toHaveLength(0);

    rerender({ url: '/api/slow' });
    unmount();

    expect(signals).toHaveLength(1);
    expect(signals[0]?.aborted).toBe(true);
    globalThis.fetch = stubbed;
  });

  it('reports a failed load without data', async () => {
    mockApi({ 'GET /api/broken': () => respond(500, { title: 'Broken' }) });
    const { result } = renderHook(() => useApi('/api/broken'));
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.data).toBeUndefined();
    expect(result.current.error).toBeInstanceOf(Error);
  });
});
