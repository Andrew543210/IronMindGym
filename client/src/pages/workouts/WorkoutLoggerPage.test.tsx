import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, useLocation } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CatalogExercise } from '../../api/types/exercises';
import type { LoggerOptions } from '../../api/types/workouts';
import type { FlashState } from '../../flash';
import { useI18n } from '../../i18n/i18n';
import { setLanguage } from '../../i18n/language';
import { SessionContext } from '../../session';
import { respond, sessionContext } from '../../test/harness';
import type { StoredDraft } from './draft';
import { WorkoutLoggerPage } from './WorkoutLoggerPage';

const benchPress: CatalogExercise = {
  exerciseId: 7, exerciseName: 'Flat Bench Press', englishName: 'Flat Bench Press', description: '', exerciseType: 'StrengthTraining',
  muscleGroupId: 1, muscleGroupName: 'Chest', muscleSubGroupId: 3, muscleSubGroupName: 'Middle Chest',
};

const rowingMachine: CatalogExercise = {
  exerciseId: 9, exerciseName: 'Rowing Machine', englishName: 'Rowing Machine', description: '', exerciseType: 'Cardio',
  muscleGroupId: 2, muscleGroupName: 'Cardio', muscleSubGroupId: 5, muscleSubGroupName: 'Machines',
};

const options: LoggerOptions = {
  activeSplitTypeName: 'Upper-Lower',
  activeSplitTypeId: 2,
  splitTypes: [{ id: 2, name: 'Upper-Lower', days: [{ id: 3, name: 'Upper' }, { id: 4, name: 'Lower' }] }],
  setTypes: [{ id: 1, name: 'Working' }, { id: 2, name: 'Warm-up' }],
  defaultSetTypeId: 1,
  exercises: [benchPress, rowingMachine],
};

const ukrainianOptions: LoggerOptions = {
  ...options,
  exercises: [{ ...benchPress, exerciseName: 'Жим лежачи на горизонтальній лаві', muscleGroupName: 'Груди', muscleSubGroupName: 'Середина грудей' }],
};

const posted: unknown[] = [];
const requests: { method: string; url: string }[] = [];
let serverDraft: unknown;
let draftPutStatus = 204;
let workoutPost: Promise<Response> | undefined;
let loggerOptions: LoggerOptions = options;

const DRAFT_ID = '0f8fad5b-d9cb-469f-a165-70867728950e';
const bench = { exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 120 };

function storeDraft(overrides: object = {}) {
  const draft = {
    draftId: DRAFT_ID, sets: [bench], timerEnd: 0, startedAt: Date.now() - 600_000, savedAt: Date.now() - 60_000, autoDuration: true,
    settings: { splitTypeId: '2', splitDayId: '3', durationMinutes: '60', reviewRating: '4', reviewComment: '' }, ...overrides,
  };
  localStorage.setItem('ironmind-workout-5', JSON.stringify(draft));
  return draft;
}

function storedDraft() {
  return JSON.parse(localStorage.getItem('ironmind-workout-5') ?? '{}') as Partial<StoredDraft>;
}

function storedSets() {
  return storedDraft().sets;
}

function SavedPage() {
  const location = useLocation();
  const { show } = useI18n();
  const message = (location.state as FlashState | null)?.message;
  return <p>{location.pathname} {message && show(message)}</p>;
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
  draftPutStatus = 204;
  workoutPost = undefined;
  loggerOptions = options;
  vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
    requests.push({ method: init?.method ?? 'GET', url });
    if (url === '/api/workouts/draft' && (init?.method ?? 'GET') === 'GET') return serverDraft ? respond(200, serverDraft) : respond(204);
    if (url.startsWith('/api/workouts/draft')) return respond(init?.method === 'PUT' ? draftPutStatus : 204);
    if (url === '/api/workouts/logger') {
      const ukrainian = (init?.headers as Record<string, string> | undefined)?.['Accept-Language'] === 'uk';
      return respond(200, ukrainian ? ukrainianOptions : loggerOptions);
    }
    if (url === '/api/workouts/previous-set?exerciseId=9') {
      return respond(200, { weightKg: 0, reps: 0, durationSeconds: 900, intensity: 7, restSeconds: 60, setTypeId: 2, localDate: '2026-11-20' });
    }
    if (url.startsWith('/api/workouts/previous-set')) return respond(204);
    if (url === '/api/session/csrf') return respond(200, { token: 'token' });
    if (url === '/api/workouts' && init?.method === 'POST') {
      posted.push(JSON.parse(init.body as string));
      return workoutPost ?? respond(200, { id: 42 });
    }
    return respond(404);
  }));
});

describe('workout logger', () => {
  it('changes the split, its day and the duration in the session settings', async () => {
    loggerOptions = {
      ...options, activeSplitTypeName: null, activeSplitTypeId: null,
      splitTypes: [...options.splitTypes, { id: 5, name: 'Full-body', days: [] }],
    };
    // Nine and a half minutes in: the elapsed time reads 10 for the half minute the test may take.
    storeDraft({ startedAt: Date.now() - 570_000 });
    const user = userEvent.setup();
    renderLogger();

    await user.click(await screen.findByText('Session settings'));
    expect(screen.getByLabelText('Duration (min)')).toHaveValue(10);
    await user.selectOptions(screen.getByLabelText('Split day'), 'Lower');
    expect(storedDraft().settings).toMatchObject({ splitTypeId: '2', splitDayId: '4' });
    await user.selectOptions(screen.getByLabelText('Split type'), 'Full-body');
    expect(screen.getByLabelText('Split day')).toHaveDisplayValue('Training Day');
    expect(storedDraft().settings).toMatchObject({ splitTypeId: '5', splitDayId: '' });

    await user.click(screen.getByLabelText('Use elapsed time'));
    expect(storedDraft()).toMatchObject({ autoDuration: false, settings: { durationMinutes: '10' } });
    await user.clear(screen.getByLabelText('Duration (min)'));
    await user.type(screen.getByLabelText('Duration (min)'), '45');
    expect(storedDraft().settings?.durationMinutes).toBe('45');
    await user.click(screen.getByLabelText('Use elapsed time'));
    expect(storedDraft().autoDuration).toBe(true);
    expect(screen.getByLabelText('Duration (min)')).toHaveValue(10);
  });

  it('starts a cardio set from the last one and logs its duration and effort', async () => {
    const user = userEvent.setup();
    renderLogger();
    await user.type(await screen.findByLabelText('Find an exercise'), 'rowing{Enter}');

    expect(await screen.findByText('Last recorded set (2026-11-20): 900 sec · effort 7/10. Reference only — adjust for today.')).toBeInTheDocument();
    expect(screen.queryByLabelText('External load (kg)')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Use these values' }));
    expect(screen.getByLabelText('Duration (sec)')).toHaveValue(900);
    expect(screen.getByLabelText('Set type')).toHaveValue('2');

    await user.selectOptions(screen.getByLabelText('Set type'), 'Working');
    await user.clear(screen.getByLabelText('Perceived effort (1–10)'));
    await user.type(screen.getByLabelText('Perceived effort (1–10)'), '8');
    await user.click(screen.getByRole('button', { name: /Log completed set/ }));

    expect(await screen.findByRole('cell', { name: 'Rowing Machine' })).toBeInTheDocument();
    expect(storedSets()).toEqual([expect.objectContaining({ exerciseId: 9, setTypeId: 1, durationSeconds: 900, intensity: 8, restSeconds: 60 })]);
  });

  it('logs a set, keeps it on the device and starts the rest timer', async () => {
    const user = userEvent.setup();
    renderLogger();

    await user.type(await screen.findByLabelText('Find an exercise'), 'bench');
    await user.click(screen.getByRole('button', { name: 'Flat Bench Press · Chest' }));
    expect(await screen.findByText('No previous set for this exercise. Start with your own values.')).toBeInTheDocument();
    await user.clear(screen.getByLabelText('External load (kg)'));
    await user.type(screen.getByLabelText('External load (kg)'), '80');
    await user.click(screen.getByRole('button', { name: 'Log completed set' }));

    expect(screen.getByText('Set logged · draft saved')).toBeInTheDocument();
    expect(screen.getByText('1 set logged')).toBeInTheDocument();
    expect(screen.getByText(/Rest timer:/).textContent).toMatch(/0[12]:\d\d/);
    expect(screen.getByRole('cell', { name: '120 sec' })).toHaveAttribute('data-label', 'Rest');
    const stored = storedDraft();
    expect(stored.sets).toEqual([{ exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 80, reps: 8, durationSeconds: 0, intensity: 0, restSeconds: 120 }]);
    expect(stored.settings).toMatchObject({ splitTypeId: '2', splitDayId: '3' });
  });

  it('explains an invalid set instead of logging it', async () => {
    const user = userEvent.setup();
    renderLogger();

    await user.selectOptions(await screen.findByLabelText('Muscle group'), 'Chest');
    await user.selectOptions(screen.getByLabelText('Exercise'), 'Flat Bench Press');
    await user.clear(screen.getByLabelText('Reps'));
    await user.click(screen.getByRole('button', { name: 'Log completed set' }));

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

  it('fetches the change token as it opens, so a change can still go out as the tab closes', async () => {
    renderLogger();

    await screen.findByRole('button', { name: 'Finish workout' });
    await waitFor(() => expect(requests.map((r) => r.url)).toContain('/api/session/csrf'));
    expect(requests.some((r) => r.method === 'PUT')).toBe(false);
  });

  it('locks the logger while the workout is being saved and opens it again when that fails', async () => {
    storeDraft();
    let fail = () => {};
    workoutPost = new Promise((resolve) => { fail = () => resolve(respond(500)); });
    const user = userEvent.setup();
    renderLogger();

    await user.click(await screen.findByRole('button', { name: 'Finish workout' }));

    expect(await screen.findByRole('button', { name: 'Saving workout…' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Log completed set' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Discard workout' })).toBeDisabled();
    act(() => fail());
    await waitFor(() => expect(screen.getByRole('button', { name: 'Log completed set' })).toBeEnabled());
    expect(storedSets()).toHaveLength(1);
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
    await waitFor(() => expect(storedDraft().draftId).toBe(draftId));
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
    await user.click(screen.getByRole('button', { name: 'Записати виконаний підхід' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Вкажіть 1–500 повторень.');
    expect(screen.getByText('Записано: 0 підходів')).toBeInTheDocument();

    act(() => setLanguage('en'));

    expect(screen.getByRole('alert')).toHaveTextContent('Enter 1–500 reps.');
    expect(await screen.findByRole('option', { name: 'Flat Bench Press' })).toBeInTheDocument();
    expect(screen.getByLabelText('Exercise')).toHaveValue('7');
  });

  it('edits a logged set, cancels an edit and removes a set after confirmation', async () => {
    storeDraft({ sets: [bench, { ...bench, weightKg: 65 }] });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const user = userEvent.setup();
    renderLogger();

    await user.click(await screen.findByRole('button', { name: 'Edit set 2' }));
    expect(screen.getByLabelText('External load (kg)')).toHaveValue(65);
    expect(screen.getByLabelText('External load (kg)')).toHaveFocus();
    await user.clear(screen.getByLabelText('External load (kg)'));
    await user.type(screen.getByLabelText('External load (kg)'), '70');
    await user.click(screen.getByRole('button', { name: 'Save changes to set 2' }));

    expect(screen.getByText('Set updated · draft saved')).toBeInTheDocument();
    expect(storedSets()?.map((s) => s.weightKg)).toEqual([60, 70]);
    expect(screen.getByText('2 sets logged')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Actions' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Edit set 1' }));
    await user.click(screen.getByRole('button', { name: 'Cancel edit' }));
    expect(screen.getByRole('button', { name: 'Log completed set' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Remove set 1' }));
    expect(window.confirm).toHaveBeenCalledWith('Remove set 1 (Flat Bench Press)?');
    expect(screen.getByText('Set removed · draft saved')).toBeInTheDocument();
    expect(storedSets()?.map((s) => s.weightKg)).toEqual([70]);
  });

  it('keeps a set when the athlete cancels its removal', async () => {
    storeDraft();
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const user = userEvent.setup();
    renderLogger();

    await user.click(await screen.findByRole('button', { name: 'Remove set 1' }));

    expect(screen.getByText('1 set logged')).toBeInTheDocument();
    expect(storedSets()).toHaveLength(1);
  });

  it('counts the rest down in the tab title, lets the athlete skip it and starts it again', async () => {
    storeDraft({ timerEnd: Date.now() + 90_000 });
    const user = userEvent.setup();
    renderLogger();

    await screen.findByText('Unsaved workout restored · 1 set');
    await waitFor(() => expect(document.title).toMatch(/^Rest 01:(29|30) · IronMind$/));
    await user.click(screen.getByRole('button', { name: 'Skip rest' }));

    expect(await screen.findByText('Rest complete. Start your next set when ready.')).toBeInTheDocument();
    await waitFor(() => expect(document.title).toBe('Workout Logger · IronMind'));
    await user.click(screen.getByRole('button', { name: 'Start timer' }));
    await waitFor(() => expect(document.title).toMatch(/^Rest 0[12]:\d\d · IronMind$/));
    expect(screen.queryByText('Rest complete. Start your next set when ready.')).not.toBeInTheDocument();
  });

  it('vibrates once when the rest is over', async () => {
    const vibrate = vi.fn();
    Object.defineProperty(navigator, 'vibrate', { value: vibrate, configurable: true });
    storeDraft();
    const user = userEvent.setup();
    renderLogger();

    await user.clear(await screen.findByLabelText('Rest (sec)'));
    await user.type(screen.getByLabelText('Rest (sec)'), '1');
    await user.click(screen.getByRole('button', { name: 'Start timer' }));
    expect(await screen.findByText('Rest complete. Start your next set when ready.', {}, { timeout: 10_000 })).toBeInTheDocument();
    await waitFor(() => expect(vibrate).toHaveBeenCalledTimes(1));
    Reflect.deleteProperty(navigator, 'vibrate');
  });

  it('follows a change made to the same workout in another tab', async () => {
    const draft = storeDraft();
    renderLogger();
    await screen.findByText('1 set logged');

    act(() => {
      localStorage.setItem('ironmind-workout-5', JSON.stringify({ ...draft, sets: [bench, bench], savedAt: Date.now() }));
      window.dispatchEvent(new StorageEvent('storage', { key: 'ironmind-workout-5' }));
    });
    expect(screen.getByText('Draft updated in another tab')).toBeInTheDocument();
    expect(screen.getByText('2 sets logged')).toBeInTheDocument();

    act(() => {
      localStorage.removeItem('ironmind-workout-5');
      window.dispatchEvent(new StorageEvent('storage', { key: 'ironmind-workout-5' }));
    });
    expect(screen.getByText('0 sets logged')).toBeInTheDocument();
    act(() => {
      window.dispatchEvent(new StorageEvent('storage', { key: 'another-key' }));
    });
    expect(screen.getByText('0 sets logged')).toBeInTheDocument();
  });

  it('starts a new workout when the server says this one was saved on another device', async () => {
    storeDraft();
    draftPutStatus = 409;
    const user = userEvent.setup();
    renderLogger();

    await user.type(await screen.findByLabelText('Find an exercise'), 'bench');
    await user.click(screen.getByRole('button', { name: 'Flat Bench Press · Chest' }));
    await user.click(screen.getByRole('button', { name: 'Log completed set' }));

    expect(await screen.findByText('This workout was already saved on another device. A new one has started.', {}, { timeout: 10_000 })).toBeInTheDocument();
    expect(screen.getByText('0 sets logged')).toBeInTheDocument();
    expect(storedDraft().draftId).not.toBe(DRAFT_ID);
  });
});
