import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { athlete, mockApi, renderPage, renderRoutes, respond } from '../../test/harness';
import { workout } from '../../test/fixtures';
import { WorkoutDetailsPage, WorkoutHistoryPage } from './WorkoutPages';

describe('workout history', () => {
  it('pages through the sessions and offers to resume an unsaved workout', async () => {
    localStorage.setItem(`ironmind-workout-${athlete.id}`, JSON.stringify({
      draftId: '0f8fad5b-d9cb-469f-a165-70867728950e', startedAt: Date.now(), savedAt: Date.now(), settings: {},
      sets: [{ exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 80, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 90 }],
    }));
    const session = (id: number) => ({ id, name: `Session ${id}`, sessionDate: '2026-11-19T17:00:00Z', durationMinutes: 45 });
    mockApi({
      'GET /api/workouts/draft': null,
      'GET /api/workouts?page=1': { sessions: [session(1)], page: 1, hasOlderSessions: true },
      'GET /api/workouts?page=2': { sessions: [session(2)], page: 2, hasOlderSessions: false },
    });
    renderPage('/workouts', <WorkoutHistoryPage />);
    const user = userEvent.setup();

    expect(await screen.findByText('You have an unsaved workout with 1 set.')).toBeInTheDocument();
    expect(await screen.findByText(/Session 1/)).toBeInTheDocument();
    expect(screen.getByText('2026-11-19 19:00')).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Older' }));

    expect(await screen.findByText(/Session 2/)).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Older' })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Newer' })).toHaveAttribute('href', '/workouts');
  });

  it('says when there are no workouts', async () => {
    mockApi({ 'GET /api/workouts/draft': null, 'GET /api/workouts?page=1': { sessions: [], page: 1, hasOlderSessions: false } });
    renderPage('/workouts', <WorkoutHistoryPage />);

    expect(await screen.findByText('No workouts yet. Start a workout to log your first sets.')).toBeInTheDocument();
  });
});

describe('workout details', () => {
  it('shows each exercise type in its own units with the session summary', async () => {
    mockApi({ 'GET /api/workouts/4': workout });
    renderPage('/workouts/:id', <WorkoutDetailsPage />, '/workouts/4');

    expect(await screen.findByText('Notes: Felt strong')).toBeInTheDocument();
    const squat = screen.getByRole('heading', { name: 'Barbell Back Squat' }).parentElement!;
    expect(within(squat).getByRole('columnheader', { name: 'Type' })).toBeInTheDocument();
    expect(within(squat).getByText('100 kg')).toBeInTheDocument();
    const rowing = screen.getByRole('heading', { name: 'Rowing Machine' }).parentElement!;
    expect(within(rowing).getByText('6/10')).toBeInTheDocument();
    expect(within(rowing).getByRole('columnheader', { name: 'Type' })).toBeInTheDocument();
    expect(within(screen.getByRole('heading', { name: 'Weighted Plank' }).parentElement!).getByText('10 kg')).toBeInTheDocument();
    expect(screen.getByText('New record')).toBeInTheDocument();
    expect(screen.getByText('No added load')).toBeInTheDocument();
    expect(screen.getByText('105.3%')).toBeInTheDocument();
    expect(screen.getByText('Great depth!')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Like' })).not.toBeInTheDocument();
  });

  it('posts a comment and deletes the workout after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    let comments = workout.comments;
    const calls = mockApi({
      'GET /api/workouts/4': () => ({ ...workout, comments }),
      'POST /api/workouts/4/comments': () => {
        comments = [...comments, { authorUsername: 'athlete', text: 'Next time 105', createdAt: '2026-11-20T08:00:00Z' }];
        return null;
      },
      'DELETE /api/workouts/4': null,
    });
    renderPage('/workouts/:id', <WorkoutDetailsPage />, '/workouts/4');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Comment'), 'Next time 105');
    await user.click(screen.getByRole('button', { name: 'Post' }));
    expect(await screen.findByText('Next time 105')).toBeInTheDocument();
    expect(screen.getByLabelText('Comment')).toHaveValue('');

    await user.click(screen.getByRole('button', { name: 'Delete workout' }));
    expect(await screen.findByTestId('location')).toHaveTextContent('/workouts Workout deleted.');
    expect(calls.filter((c) => c.method !== 'GET').map((c) => c.method)).toEqual(['POST', 'DELETE']);
  });

  it('shows that a workout does not exist', async () => {
    mockApi({ 'GET /api/workouts/99': () => respond(404) });
    renderRoutes([{ path: '/workouts/:id', element: <WorkoutDetailsPage /> }], '/workouts/99');

    expect(await screen.findByRole('alert')).toHaveTextContent('Not found. It may have been deleted.');
  });
});
