import { describe, expect, it, vi } from 'vitest';
import { ApiError, api } from './api';

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
    const [, init] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect((init.headers as Record<string, string>)['X-CSRF-TOKEN']).toBe('token-1');
  });

  it('retries once with a new token when the old one was refused', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json(200, { token: 'old' }))
      .mockResolvedValueOnce(new Response(null, { status: 400 }))
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
});
