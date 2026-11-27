import { describe, expect, it, vi } from 'vitest';
import { ApiError, CSRF_PROBLEM, api } from './client';

function json(status: number, body: unknown, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } });
}

describe('api', () => {
  it('sends the CSRF token with changes and turns problem details into messages', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json(200, { token: 'token-1' }))
      .mockResolvedValueOnce(json(400, { title: 'One or more validation errors occurred.', errors: { '': ['Choose a valid training split.'] } }, 'application/problem+json'));
    vi.stubGlobal('fetch', fetchMock);

    const error = await api.post('/api/setup', { splitTypeId: 0 }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).messages).toEqual(['Choose a valid training split.']);
    // A refused value is not a token problem, so the request is not sent again.
    expect(fetchMock).toHaveBeenCalledTimes(2);
    const [, init] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect((init.headers as Record<string, string>)['X-CSRF-TOKEN']).toBe('token-1');
  });

  it('retries once with a new token when the old one was refused', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json(200, { token: 'old' }))
      .mockResolvedValueOnce(json(400, { type: CSRF_PROBLEM, title: 'The request could not be verified.' }, 'application/problem+json'))
      .mockResolvedValueOnce(json(200, { token: 'new' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await api.post('/api/goals', { title: 'Run 5 km' });

    expect(fetchMock).toHaveBeenCalledTimes(4);
    const [, init] = fetchMock.mock.calls[3] as [string, RequestInit];
    expect((init.headers as Record<string, string>)['X-CSRF-TOKEN']).toBe('new');
  });

  it('reports a lost connection in plain words', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    await expect(api.get('/api/dashboard')).rejects.toThrow('No connection to the server.');
  });

  it('passes a cancelled request on as cancelled, not as a lost connection', async () => {
    const aborted = new DOMException('The operation was aborted.', 'AbortError');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(aborted));
    const controller = new AbortController();
    controller.abort();

    await expect(api.get('/api/dashboard', { signal: controller.signal })).rejects.toBe(aborted);
  });

  it('asks the browser to finish a small request after the tab closes, and sends a large one the ordinary way', async () => {
    const fetchMock = vi.fn<(url: string, init?: RequestInit) => Promise<Response>>(async (url) =>
      (url === '/api/session/csrf' ? json(200, { token: 't' }) : new Response(null, { status: 204 })));
    vi.stubGlobal('fetch', fetchMock);

    await api.put('/api/workouts/draft', { note: 'short' }, { keepalive: true });
    await api.put('/api/workouts/draft', { note: 'я'.repeat(40_000) }, { keepalive: true });

    const sent = fetchMock.mock.calls.slice(1).map(([, init]) => init?.keepalive);
    expect(sent).toEqual([true, false]);
  });
});
