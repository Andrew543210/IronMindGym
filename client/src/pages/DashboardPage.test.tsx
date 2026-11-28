import { act, cleanup, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it } from 'vitest';
import { currentLanguage, setLanguage } from '../i18n/language';
import { mockApi, renderRoutes, respond } from '../test/harness';
import { overview, researchNote } from '../test/fixtures';
import { DashboardPage, FeedNotePage } from './DashboardPage';

const routes = [{ path: '/', element: <DashboardPage /> }, { path: '/feed/:id', element: <FeedNotePage /> }];

it('brings the week, rank, sessions, nutrition, goals and research together and opens a note', async () => {
  mockApi({
    'GET /api/dashboard': overview,
    'GET /api/feed/7': researchNote,
  });
  renderRoutes(routes, '/');
  const user = userEvent.setup();

  expect(await screen.findByText("Let's build, athlete.")).toBeInTheDocument();
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent("Let's build, athlete.");
  expect(screen.getByRole('heading', { level: 2, name: 'Recent sessions' })).toBeInTheDocument();
  expect(screen.getByRole('img', { name: 'Weekly completion 66%' })).toBeInTheDocument();
  expect(screen.getAllByText('Gym Centurion')).toHaveLength(2);
  expect(screen.getByRole('meter', { name: 'Position on the rank scale' })).toHaveAttribute('aria-valuenow', '60');
  expect(screen.getAllByText('Gym Centurion')[1]?.closest('.rank-item')).toHaveAttribute('aria-current', 'true');
  expect(screen.getByText('Noodle Nomad').closest('.rank-item')).not.toHaveAttribute('aria-current');
  expect(screen.getByRole('link', { name: /Legs day/ })).toHaveAttribute('href', '/workouts/4');
  expect(screen.getByText('2100')).toBeInTheDocument();
  expect(screen.getByText('Squat 120 kg')).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Read original source' })).toHaveAttribute('href', 'https://example.org/rest');

  await user.click(screen.getByRole('link', { name: 'View note' }));

  expect(await screen.findByText('Source publication year: 2021')).toBeInTheDocument();
  expect(screen.getByText('Journal of Strength')).toBeInTheDocument();
});

it('greets a new athlete with empty sections', async () => {
  mockApi({
    'GET /api/dashboard': {
      ...overview, needsSetup: true, weightKg: null, todayNutrition: null, goals: [], recentWorkouts: [], plannedWorkoutsPerWeek: 0, feedItems: [],
    },
  });
  renderRoutes(routes, '/');

  expect(await screen.findByText('Make this space yours.')).toBeInTheDocument();
  expect(screen.getByText('Your journal is ready.')).toBeInTheDocument();
  expect(screen.getByText('Give your next session a direction.')).toBeInTheDocument();
  expect(screen.getByRole('img', { name: 'Weekly completion 0%' })).toBeInTheDocument();
});

it('heads the page while it loads or fails and keeps a note on screen while its translation loads', async () => {
  let translated = () => {};
  mockApi({
    'GET /api/dashboard': () => respond(500),
    'GET /api/feed/7': () => (currentLanguage() === 'uk'
      ? new Promise((resolve) => { translated = () => resolve({ ...researchNote, title: 'Українська назва' }); })
      : researchNote),
  });
  renderRoutes(routes, '/');
  expect(await screen.findByRole('alert')).toBeInTheDocument();
  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Overview');
  cleanup();

  renderRoutes(routes, '/feed/7');
  expect(await screen.findByText(researchNote.title)).toBeInTheDocument();
  act(() => setLanguage('uk'));
  expect(screen.getByText(researchNote.title)).toBeInTheDocument();
  act(() => translated());
  expect(await screen.findByText('Українська назва')).toBeInTheDocument();
});
