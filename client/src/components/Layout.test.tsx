import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Link } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { renderRoutes } from '../test/harness';
import { Layout } from './Layout';

function app() {
  return renderRoutes([{
    element: <Layout />,
    children: [
      { path: '/goals', element: <Link to="/awards">Awards</Link> },
      { path: '/awards', element: <p>Awards page</p> },
    ],
  }], '/awards');
}

describe('layout', () => {
  it('shows a message once and takes it out of the history entry', async () => {
    const router = app();
    await act(() => router.navigate('/goals', { state: { message: 'Goal saved.', from: '/awards' } }));

    expect(await screen.findByText('Goal saved.')).toBeInTheDocument();
    await vi.waitFor(() => expect(router.state.location.state).toEqual({ from: '/awards' }));
    expect(screen.getByText('Goal saved.')).toBeInTheDocument();

    await userEvent.setup().click(screen.getByRole('link', { name: 'Awards' }));
    expect(await screen.findByText('Awards page')).toBeInTheDocument();
    expect(screen.queryByText('Goal saved.')).not.toBeInTheDocument();

    await act(() => router.navigate(-1));
    expect(await screen.findByRole('link', { name: 'Awards' })).toBeInTheDocument();
    expect(screen.queryByText('Goal saved.')).not.toBeInTheDocument();
  });

  it('shows errors as alerts', async () => {
    const router = app();
    await act(() => router.navigate('/goals', { state: { error: 'That goal no longer exists.' } }));

    expect(await screen.findByRole('alert')).toHaveTextContent('That goal no longer exists.');
    await vi.waitFor(() => expect(router.state.location.state).toBeNull());
  });
});
