import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, useLocation } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setLanguage } from '../../i18n/language';
import { SessionContext } from '../../session';
import { respond, sessionContext } from '../../test/harness';
import { WorkoutLoggerPage, type LoggerOptions } from './WorkoutLoggerPage';

const options: LoggerOptions = {
  activeSplitTypeName: 'Upper-Lower',
  activeSplitTypeId: 2,
  splitTypes: [{ id: 2, name: 'Upper-Lower', days: [{ id: 3, name: 'Upper' }, { id: 4, name: 'Lower' }] }],
  setTypes: [{ id: 1, name: 'Working' }, { id: 2, name: 'Warm-up' }],
  defaultSetTypeId: 1,
  muscleGroups: [{ id: 1, name: 'Chest' }],
  exercises: [{
    exerciseId: 7, exerciseName: 'Flat Bench Press', englishName: 'Flat Bench Press', exerciseType: 'StrengthTraining',
    muscleGroupId: 1, muscleGroupName: 'Chest', muscleSubGroupId: 3, muscleSubGroupName: 'Middle Chest',
  }],
};

const ukrainianOptions: LoggerOptions = {
  ...options,
  muscleGroups: [{ id: 1, name: 'Груди' }],
  exercises: [{ ...options.exercises[0], exerciseName: 'Жим лежачи на горизонтальній лаві', muscleGroupName: 'Груди', muscleSubGroupName: 'Середина грудей' }],
};

const posted: unknown[] = [];
const requests: { method: string; url: string }[] = [];
let serverDraft: unknown;

function SavedPage() {
  const location = useLocation();
  return <p>{location.pathname} {(location.state as { message?: string } | null)?.message}</p>;
}

function renderLogger() {
  const router = createMemoryRouter([
    { path: '/workouts/new', element: <WorkoutLoggerPage /> },
    { path: '/workouts/:id', element: <SavedPage /> },
  ], { initialEntries: ['/workouts/new'] });
  render(<SessionContext.Provider value={sessionContext()}><RouterProvider router={router} /></SessionContext.Provider>);
}

beforeEach(() => {
  posted.length = 0;
  requests.length = 0;
  serverDraft = undefined;
  vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
    requests.push({ method: init?.method ?? 'GET', url });
    if (url === '/api/workouts/draft' && (init?.method ?? 'GET') === 'GET') return serverDraft ? respond(200, serverDraft) : respond(204);
    if (url.startsWith('/api/workouts/draft')) return respond(204);
    if (url === '/api/workouts/logger') {
      const ukrainian = (init?.headers as Record<string, string> | undefined)?.['Accept-Language'] === 'uk';
      return respond(200, ukrainian ? ukrainianOptions : options);
    }
    if (url.startsWith('/api/workouts/previous-set')) return respond(204);
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    if (url === '/api/workouts' && init?.method === 'POST') {
      posted.push(JSON.parse(String(init.body)));
      return respond(200, { id: 42 });
    }
    return respond(404);
  }));
});

describe('workout logger', () => {
  it('logs a set, keeps it on the device and starts the rest timer', async () => {
    const user = userEvent.setup();
    renderLogger();

    await user.type(await screen.findByLabelText('Find an exercise'), 'bench');
    await user.click(screen.getByRole('button', { name: 'Flat Bench Press · Chest' }));
    expect(await screen.findByText('No previous set for this exercise. Start with your own values.')).toBeInTheDocument();
    await user.clear(screen.getByLabelText('External load (kg)'));
    await user.type(screen.getByLabelText('External load (kg)'), '80');
    await user.click(screen.getByRole('button', { name: 'Log completed set ＋' }));

    expect(screen.getByText('Set logged · draft saved')).toBeInTheDocument();
    expect(screen.getByText('1 set logged')).toBeInTheDocument();
    expect(screen.getByText(/Rest timer:/).textContent).toMatch(/0[12]:\d\d/);
    const stored = JSON.parse(localStorage.getItem('ironmind-workout-5')!);
    expect(stored.sets).toEqual([{ exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 80, reps: 8, durationSeconds: 0, intensity: 0, restSeconds: 120 }]);
    expect(stored.settings).toMatchObject({ splitTypeId: '2', splitDayId: '3' });
  });

  it('explains an invalid set instead of logging it', async () => {
    const user = userEvent.setup();
    renderLogger();

    await user.selectOptions(await screen.findByLabelText('Muscle group'), 'Chest');
    await user.selectOptions(screen.getByLabelText('Exercise'), 'Flat Bench Press');
    await user.clear(screen.getByLabelText('Reps'));
    await user.click(screen.getByRole('button', { name: 'Log completed set ＋' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Enter 1–500 reps.');
    expect(screen.getByText('0 sets logged')).toBeInTheDocument();
  });

  it('finishes a workout kept in this browser and clears the draft', async () => {
    localStorage.setItem('ironmind-workout-5', JSON.stringify({
      draftId: '0f8fad5b-d9cb-469f-a165-70867728950e',
      sets: [{ exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 120 }],
      timerEnd: 0,
      startedAt: Date.now() - 600_000,
      autoDuration: false,
      settings: { splitDayId: '4', durationMinutes: '45', reviewRating: '5', reviewComment: 'Heavy day' },
    }));
    const user = userEvent.setup();
    renderLogger();

    expect(await screen.findByText('Unsaved workout restored · 1 set')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Finish workout' }));

    expect(await screen.findByText('/workouts/42 Workout saved. Keep showing up.')).toBeInTheDocument();
    expect(posted).toEqual([expect.objectContaining({
      draftId: '0f8fad5b-d9cb-469f-a165-70867728950e',
      splitDayId: 4,
      durationMinutes: 45,
      reviewRating: 5,
      reviewComment: 'Heavy day',
    })]);
    await waitFor(() => expect(localStorage.getItem('ironmind-workout-5')).toBeNull());
  });

  it('continues a workout started on another device and removes it there when discarded', async () => {
    const draftId = '0f8fad5b-d9cb-469f-a165-70867728950e';
    const logged = { exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 120 };
    serverDraft = {
      draftId,
      updatedAt: '2026-11-20T10:00:00Z',
      content: { draftId, sets: [logged, logged], timerEnd: 0, startedAt: Date.now() - 600_000, savedAt: Date.now() - 60_000, autoDuration: true, settings: {} },
    };
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const user = userEvent.setup();
    renderLogger();

    expect(await screen.findByText('Unsaved workout restored · 2 sets')).toBeInTheDocument();
    await waitFor(() => expect(JSON.parse(localStorage.getItem('ironmind-workout-5') ?? '{}').draftId).toBe(draftId));
    await user.click(screen.getByRole('button', { name: 'Discard workout' }));

    await waitFor(() => expect(requests).toContainEqual({ method: 'DELETE', url: `/api/workouts/draft?draftId=${draftId}` }));
    expect(screen.getByText('0 sets logged')).toBeInTheDocument();
  });

  it('works in Ukrainian, finds an exercise by its English name and follows a change of language', async () => {
    setLanguage('uk');
    const user = userEvent.setup();
    renderLogger();

    await user.type(await screen.findByLabelText('Знайти вправу'), 'bench');
    await user.click(screen.getByRole('button', { name: 'Жим лежачи на горизонтальній лаві · Груди' }));
    await user.clear(screen.getByLabelText('Повторення'));
    await user.click(screen.getByRole('button', { name: 'Записати виконаний підхід ＋' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Вкажіть 1–500 повторень.');
    expect(screen.getByText('Записано: 0 підходів')).toBeInTheDocument();

    act(() => setLanguage('en'));

    expect(screen.getByRole('alert')).toHaveTextContent('Enter 1–500 reps.');
    expect(await screen.findByRole('option', { name: 'Flat Bench Press' })).toBeInTheDocument();
    expect(screen.getByLabelText('Exercise')).toHaveValue('7');
  });
});
