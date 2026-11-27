import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Link } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { flash } from '../flash';
import { setLanguage } from '../i18n/language';
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
    await act(() => router.navigate('/goals', { state: { ...flash({ key: 'settings.saved' }), from: '/awards' } }));

    expect(await screen.findByText('Profile updated.')).toBeInTheDocument();
    await vi.waitFor(() => expect(router.state.location.state).toEqual({ from: '/awards' }));
    expect(screen.getByText('Profile updated.')).toBeInTheDocument();

    await userEvent.setup().click(screen.getByRole('link', { name: 'Awards' }));
    expect(await screen.findByText('Awards page')).toBeInTheDocument();
    expect(screen.queryByText('Profile updated.')).not.toBeInTheDocument();

    await act(() => router.navigate(-1));
    expect(await screen.findByRole('link', { name: 'Awards' })).toBeInTheDocument();
    expect(screen.queryByText('Profile updated.')).not.toBeInTheDocument();
  });

  it('words the message in the language chosen while it is shown', async () => {
    const router = app();
    await act(() => router.navigate('/goals', { state: flash({ key: 'login.welcome', params: { name: 'athlete' } }) }));
    expect(await screen.findByText('Welcome, athlete!')).toBeInTheDocument();

    act(() => setLanguage('uk'));
    expect(await screen.findByText('Вітаємо, athlete!')).toBeInTheDocument();
  });
});
