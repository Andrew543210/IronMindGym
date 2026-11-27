import { render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { describe, expect, it, vi } from 'vitest';
import { RouteError } from './RouteError';

function openFailing(failure: () => never) {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  vi.spyOn(console, 'warn').mockImplementation(() => {});
  const router = createMemoryRouter([
    { path: '/', element: <p>Overview</p> },
    { path: '/broken', errorElement: <RouteError />, lazy: async () => failure() },
  ], { initialEntries: ['/broken'] });
  render(<RouterProvider router={router} />);
}

describe('route errors', () => {
  // Chrome, Firefox and Safari word the failed import of a removed bundle differently.
  it.each([
    'Failed to fetch dynamically imported module: /assets/GoalsPage-1a2b.js',
    'error loading dynamically imported module: https://ironmind.example/assets/GoalsPage-1a2b.js',
    'Importing a module script failed.',
  ])('asks to reload a tab whose section bundle was removed by a release: %s', async (message) => {
    openFailing(() => {
      throw new TypeError(message);
    });

    expect(await screen.findByRole('alert')).toHaveTextContent('IronMind has been updated since this tab was opened.');
    expect(screen.getByRole('button', { name: 'Reload the page' })).toBeInTheDocument();
    await waitFor(() => expect(document.title).toBe('IronMind was updated · IronMind'));
  });

  it('offers a way back from any other failure', async () => {
    openFailing(() => {
      throw new Error('Rendering failed');
    });

    expect(await screen.findByRole('alert')).toHaveTextContent('This page could not be shown.');
    expect(screen.getByRole('link', { name: 'Back to the overview' })).toHaveAttribute('href', '/');
  });
});
