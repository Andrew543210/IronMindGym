import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { expect, it, vi } from 'vitest';
import { Layout } from '../components/Layout';
import { SessionContext } from '../session';
import { respond, sessionContext } from '../test/harness';
import { FriendsPage } from './FriendsPage';

function card(requestSent: boolean) {
  return {
    userId: 9, username: 'partner', isFriend: false, requestSent, requestReceived: false, averageKcal: 0,
    lastFiveWorkouts: [], lastNutritionLogs: [], personalRecords: [], achievements: [],
  };
}

it('searches by username, sends a request and shows the new status', async () => {
  let sent = false;
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    if (url === '/api/friends/9/request' && init?.method === 'POST') {
      sent = true;
      return respond(200, { result: 'Sent' });
    }
    if (url === '/api/friends?search=part') return respond(200, { friends: [card(sent)] });
    if (url === '/api/friends?search=') return respond(200, { friends: [] });
    return respond(404);
  });
  vi.stubGlobal('fetch', fetchMock);
  const router = createMemoryRouter([{ element: <Layout />, children: [{ path: '/friends', element: <FriendsPage /> }] }], { initialEntries: ['/friends'] });
  render(<SessionContext.Provider value={sessionContext()}><RouterProvider router={router} /></SessionContext.Provider>);
  const user = userEvent.setup();

  expect(await screen.findByText('No connections yet. Search for a username to invite a training partner.')).toBeInTheDocument();
  await user.type(screen.getByLabelText('Username'), 'part');
  await user.click(screen.getByRole('button', { name: 'Search' }));
  await user.click(await screen.findByRole('button', { name: 'Add friend' }));

  expect(await screen.findByText('Friend request sent.')).toBeInTheDocument();
  expect(await screen.findByRole('button', { name: 'Cancel request' })).toBeInTheDocument();
  expect(screen.getByText('Status: Request sent')).toBeInTheDocument();
});
