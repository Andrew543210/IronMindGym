import type { RouteObject } from 'react-router';
import { describe, expect, it } from 'vitest';
import { routes } from './routes';

function lazyRoutes(list: RouteObject[]): RouteObject[] {
  return list.flatMap((route) => [...(route.lazy ? [route] : []), ...lazyRoutes(route.children ?? [])]);
}

describe('routes', () => {
  it('loads a page component for every section', async () => {
    const sections = lazyRoutes(routes);
    expect(sections).toHaveLength(20);
    for (const route of sections) {
      const loaded = await (route.lazy as () => Promise<RouteObject>)();
      expect(typeof loaded.Component, route.path).toBe('function');
    }
  });
});
