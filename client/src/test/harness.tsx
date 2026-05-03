import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import { createMemoryRouter, useLocation, type RouteObject } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { vi } from 'vitest';
import { SessionContext, type Session, type SessionContextValue } from '../session';

export const athlete: Session = {
  id: 5, username: 'athlete', email: 'athlete@example.com', timeZone: 'Europe/Kyiv', today: '2026-11-20', setupComplete: true, language: 'en',
};

export function sessionContext(session: Session | null = athlete, overrides: Partial<SessionContextValue> = {}): SessionContextValue {
  return {
    loaded: true, endedBy: null, session,
    signedIn: () => {}, signedOut: () => {}, signOut: async () => {}, refresh: async () => {},
    ...overrides,
  };
}

export function respond(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

export interface ApiCall {
  method: string;
  url: string;
  body: unknown;
  headers: Record<string, string>;
}

type Reply = Response | object | null | (() => Response | object | null);

/**
 * Answers fetch from a table keyed by "METHOD /url". A plain value becomes a 200 JSON response, null a 204,
 * a missing route a 404. The antiforgery token is always available; every other request is recorded in order.
 */
export function mockApi(routes: Record<string, Reply>) {
  const calls: ApiCall[] = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    const method = init.method ?? 'GET';
    const body = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    calls.push({ method, url, body, headers: (init.headers ?? {}) as Record<string, string> });
    const route = routes[`${method} ${url}`];
    if (route === undefined) return respond(404);
    const reply = typeof route === 'function' ? route() : route;
    if (reply instanceof Response) return reply;
    return reply === null ? respond(204) : respond(200, reply);
  }));
  return calls;
}

function CurrentLocation() {
  const location = useLocation();
  const state = location.state as { message?: string } | null;
  return <p data-testid="location">{location.pathname}{location.search}{state?.message ? ` ${state.message}` : ''}</p>;
}

/** Renders the given routes in memory; any other path shows where the page navigated and its flash message. */
export function renderRoutes(routes: RouteObject[], at: string, session: SessionContextValue = sessionContext()) {
  const router = createMemoryRouter([...routes, { path: '*', element: <CurrentLocation /> }], { initialEntries: [at] });
  render(<SessionContext.Provider value={session}><RouterProvider router={router} /></SessionContext.Provider>);
  return router;
}

export function renderPage(path: string, element: ReactNode, at = path, session?: SessionContextValue) {
  return renderRoutes([{ path, element }], at, session);
}
