import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { describe, expect, it, vi } from 'vitest';
import { Layout } from '../../components/Layout';
import { progressEntry } from '../../test/fixtures';
import { mockApi, renderPage, renderRoutes, respond } from '../../test/harness';
import { ProgressPage } from './ProgressPage';
import { TrackExercisePage } from './TrackExercisePage';

it('loads the saved results, compares them while typing and saves both columns', async () => {
  const saved: unknown[] = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
    if (url === '/api/progress/catalog') {
      return respond(200, [{
        exerciseId: 9, exerciseName: 'Weighted Plank', exerciseType: 'Timed',
        muscleGroupId: 4, muscleGroupName: 'Core', muscleSubGroupId: 6, muscleSubGroupName: 'Abs',
      }]);
    }
    if (url === '/api/progress/9' && !init?.method?.startsWith('P')) return respond(200, { initialValue1: 60, initialValue2: 10, currentValue1: 75, currentValue2: 10 });
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    if (url === '/api/progress/9' && init?.method === 'PUT') {
      saved.push(JSON.parse(String(init.body)));
      return respond(204);
    }
    return respond(404);
  }));
  const router = createMemoryRouter([{ path: '/progress/track', element: <TrackExercisePage /> }], { initialEntries: ['/progress/track?exerciseId=9'] });
  render(<RouterProvider router={router} />);
  const user = userEvent.setup();

  expect(await screen.findByText('+15 sec duration; 0 kg load. Compare under similar conditions.')).toBeInTheDocument();
  const currentLoad = within(screen.getByRole('group', { name: 'Current results' })).getByLabelText('External load (kg)');
  await user.clear(currentLoad);
  await user.type(currentLoad, '12.5');
  expect(screen.getByText('+15 sec duration; +2.5 kg load. Compare under similar conditions.')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Set result' }));

  expect(await screen.findByText('Results saved. Your next session starts from here.')).toBeInTheDocument();
  expect(saved).toEqual([{ initialValue1: 60, initialValue2: 10, currentValue1: 75, currentValue2: 12.5 }]);
});

describe('progress tracker', () => {
  it('updates a current result, keeps cards without one and deletes a tracker', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    let entries = [
      progressEntry(),
      progressEntry({ exerciseId: 9, exerciseName: 'Weighted Plank', muscleGroup: 'Core', muscleSubGroup: 'Abs', exerciseType: 'Timed', hasCurrentResult: false, initialDurationSeconds: 60, initialWeightKg: 10 }),
    ];
    const calls = mockApi({
      'GET /api/progress': () => entries,
      'PUT /api/progress/7/current': null,
      'DELETE /api/progress/9': () => {
        entries = entries.filter((e) => e.exerciseId !== 9);
        return null;
      },
    });
    renderRoutes([{ element: <Layout />, children: [{ path: '/progress', element: <ProgressPage /> }] }], '/progress');
    const user = userEvent.setup();

    const plank = (await screen.findByText('Weighted Plank')).closest('.card') as HTMLElement;
    expect(within(plank).getByText('Not recorded')).toBeInTheDocument();
    expect(within(plank).getByText('Add current result')).toBeInTheDocument();
    const bench = screen.getByText('Flat Bench Press').closest('.card') as HTMLElement;
    await user.click(within(bench).getByRole('button', { name: 'Set Result' }));
    expect(within(bench).getByRole('alert')).toHaveTextContent('Enter both values.');
    await user.type(within(bench).getByLabelText('New weight in kg'), '72.5');
    await user.type(within(bench).getByLabelText('New reps'), '6');
    await user.click(within(bench).getByRole('button', { name: 'Set Result' }));
    await vi.waitFor(() => expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({ weightKg: 72.5, reps: 6 }));

    await user.click(within(plank).getByRole('button', { name: 'Delete' }));
    expect(await screen.findByText('Progress tracker deleted.')).toBeInTheDocument();
    await vi.waitFor(() => expect(screen.queryByText('Weighted Plank')).not.toBeInTheDocument());
  });

  it('invites the first tracker', async () => {
    mockApi({ 'GET /api/progress': [] });
    renderPage('/progress', <ProgressPage />);

    expect(await screen.findByText('No progress tracked yet. Choose “Add exercise progress” to start!')).toBeInTheDocument();
  });
});
