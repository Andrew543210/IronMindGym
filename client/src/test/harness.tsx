import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import { createMemoryRouter, useLocation, type RouteObject } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { vi } from 'vitest';
import type { Session } from '../api/types/session';
import type { FlashState } from '../flash';
import { useI18n } from '../i18n/i18n';
import { SessionContext, type SessionContextValue } from '../session';

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

type Answer = Response | object | null;
type Later = () => Answer | Promise<Answer>;
type Reply = Answer | Later;

function isLater(reply: Reply): reply is Later {
  return typeof reply === 'function';
}

export const unmocked: string[] = [];

// A plain value answers 200 with JSON, null 204, and a function may answer later; a request without a route
// gets a 404 and fails the test once it ends.
export function mockApi(routes: Record<string, Reply>) {
  const calls: ApiCall[] = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    const method = init.method ?? 'GET';
    const body = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    calls.push({ method, url, body, headers: (init.headers ?? {}) as Record<string, string> });
    const route = routes[`${method} ${url}`];
    if (route === undefined) {
      unmocked.push(`${method} ${url}`);
      return respond(404);
    }
    const reply = isLater(route) ? await route() : route;
    if (reply instanceof Response) return reply;
    return reply === null ? respond(204) : respond(200, reply);
  }));
  return calls;
}

function CurrentLocation() {
  const location = useLocation();
  const { show } = useI18n();
  const state = location.state as FlashState | null;
  return <p data-testid="location">{location.pathname}{location.search}{state?.message ? ` ${show(state.message)}` : ''}</p>;
}

export function renderRoutes(routes: RouteObject[], at: string, session: SessionContextValue = sessionContext()) {
  const router = createMemoryRouter([...routes, { path: '*', element: <CurrentLocation /> }], { initialEntries: [at] });
  render(<SessionContext.Provider value={session}><RouterProvider router={router} /></SessionContext.Provider>);
  return router;
}

export function renderPage(path: string, element: ReactNode, at = path, session?: SessionContextValue) {
  return renderRoutes([{ path, element }], at, session);
}
