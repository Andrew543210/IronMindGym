import { describe, expect, it } from 'vitest';
import { translate, type MessageKey, type Params } from '../../i18n/i18n';
import {
  EMPTY_DRAFT_LIFETIME_MS, applyStored, draftStore, elapsedMinutes, finishError, freshDraft, isDraftId, newDraftId,
  newerDraft, readSet, restoreDraft, setCells, startingFields, validSets, type CatalogExercise, type DraftSet, type DraftSettings,
} from './draft';

const t = (key: MessageKey, params?: Params) => translate(key, params, 'en');
const english = (key: MessageKey | null | undefined) => (key ? t(key) : '');

const defaults: DraftSettings = { splitTypeId: '2', splitDayId: '3', durationMinutes: '60', reviewRating: '4', reviewComment: '' };

const bench: CatalogExercise = {
  exerciseId: 7, exerciseName: 'Flat Bench Press', englishName: 'Flat Bench Press', exerciseType: 'StrengthTraining',
  muscleGroupId: 1, muscleGroupName: 'Chest', muscleSubGroupId: 3, muscleSubGroupName: 'Middle Chest',
};
const rowing: CatalogExercise = { ...bench, exerciseId: 8, exerciseName: 'Rowing Machine', exerciseType: 'Cardio' };
const plank: CatalogExercise = { ...bench, exerciseId: 9, exerciseName: 'Weighted Plank', exerciseType: 'Timed' };

function set(overrides: Partial<DraftSet> = {}): DraftSet {
  return { exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 120, ...overrides };
}

const fields = { setTypeId: '1', weightKg: '60', reps: '5', durationSeconds: '60', intensity: '5', restSeconds: '90' };

describe('draft store', () => {
  it('keeps one draft per athlete in local storage', () => {
    const store = draftStore(5);

    store.write({ ...freshDraft(defaults), sets: [set()] });

    expect(store.key).toBe('ironmind-workout-5');
    expect(store.read()?.sets).toEqual([set()]);
    expect(draftStore(6).read()).toBeNull();
    store.clear();
    expect(store.read()).toBeNull();
  });

  it('treats damaged storage as no draft', () => {
    localStorage.setItem('ironmind-workout-5', '{not json');
    expect(draftStore(5).read()).toBeNull();
    expect(validSets([null, { exerciseId: 'x' }, set()])).toEqual([set()]);
    expect(validSets('nope')).toEqual([]);
  });
});

describe('restoring a draft', () => {
  const now = Date.UTC(2026, 10, 20, 18, 0);

  it('continues stored sets with the stored settings, even without a split id', () => {
    const stored = {
      draftId: '0f8fad5b-d9cb-469f-a165-70867728950e', sets: [set()], timerEnd: now + 30_000, startedAt: now - 600_000,
      autoDuration: false, settings: { splitDayId: '4', durationMinutes: '45', reviewRating: '5', reviewComment: 'Heavy' },
    };

    const draft = restoreDraft(stored, defaults, now);

    expect(draft.draftId).toBe(stored.draftId);
    expect(draft.sets).toHaveLength(1);
    expect(draft.timerEnd).toBe(now + 30_000);
    expect(draft.autoDuration).toBe(false);
    expect(draft.settings).toEqual({ splitTypeId: '2', splitDayId: '4', durationMinutes: '45', reviewRating: '5', reviewComment: 'Heavy' });
  });

  it('keeps the clock of an empty draft for two hours, then starts fresh', () => {
    const recent = restoreDraft({ startedAt: now - 60_000, sets: [] }, defaults, now);
    const old = restoreDraft({ startedAt: now - EMPTY_DRAFT_LIFETIME_MS - 1, sets: [] }, defaults, now);

    expect(recent.startedAt).toBe(now - 60_000);
    expect(old.startedAt).toBe(now);
    expect(isDraftId(old.draftId)).toBe(true);
  });

  it('ignores a start time in the future and a broken draft id', () => {
    const base = freshDraft(defaults, now);
    const draft = applyStored({ startedAt: now + 5000, draftId: 'abc', sets: [set()] }, base, now);

    expect(draft.startedAt).toBe(now);
    expect(draft.draftId).toBe(base.draftId);
  });

  it('creates version 4 ids', () => {
    expect(newDraftId()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
  });
});

describe('reading a set', () => {
  it('builds a strength set and starts the next set from the last one of the same exercise', () => {
    const result = readSet(bench, { ...fields, weightKg: '62.5' }, 'Working');

    expect(result.set).toEqual(set({ weightKg: 62.5, restSeconds: 90 }));
    expect(startingFields([set({ weightKg: 70 }), set({ exerciseId: 8 })], 7, 1).weightKg).toBe('70');
    expect(startingFields([], 7, 3)).toEqual({ setTypeId: '3', weightKg: '0', reps: '8', durationSeconds: '60', intensity: '5', restSeconds: '120' });
  });

  it('keeps only the values that belong to cardio and timed work', () => {
    expect(readSet(rowing, fields, 'Working').set).toMatchObject({ weightKg: 0, reps: 0, durationSeconds: 60, intensity: 5 });
    expect(readSet(plank, fields, 'Working').set).toMatchObject({ weightKg: 60, reps: 0, durationSeconds: 60, intensity: 0 });
  });

  it.each([
    [bench, { weightKg: '' }, 'Enter an external load of 0–500 kg (0 = no added load).'],
    [bench, { weightKg: '501' }, 'Enter an external load of 0–500 kg (0 = no added load).'],
    [bench, { reps: '0' }, 'Enter 1–500 reps.'],
    [bench, { reps: '2.5' }, 'Enter 1–500 reps.'],
    [plank, { durationSeconds: '0' }, 'Enter a duration of 1–86400 seconds.'],
    [rowing, { intensity: '11' }, 'Enter perceived effort from 1 (very easy) to 10 (maximal).'],
    [bench, { restSeconds: '3601' }, 'Rest must be 0–3600 seconds.'],
    [bench, { restSeconds: '' }, 'Rest must be 0–3600 seconds.'],
  ])('rejects %#', (exercise, change, message) => {
    expect(english(readSet(exercise, { ...fields, ...change }, 'Working').error)).toBe(message);
  });

  it('asks for an exercise first', () => {
    expect(english(readSet(undefined, fields, 'Working').error)).toBe('Choose an exercise and set type first.');
  });
});

describe('showing and finishing', () => {
  it('describes sets by exercise type', () => {
    expect(setCells(set(), bench, t)).toEqual(['Flat Bench Press', 'Working', '60 kg', '5 reps', '120 sec']);
    expect(setCells(set({ reps: 1 }), bench, t)[3]).toBe('1 rep');
    expect(setCells(set({ durationSeconds: 300, intensity: 7 }), rowing, t).slice(2, 4)).toEqual(['300 sec', '7/10 effort']);
    expect(setCells(set({ durationSeconds: 45, weightKg: 10 }), plank, t).slice(2, 4)).toEqual(['45 sec', '10 kg']);
    expect(setCells(set(), undefined, t)[0]).toBe('Exercise');
    expect(setCells(set(), bench, t, 'Робочий')[1]).toBe('Робочий');
  });

  it('rounds the elapsed time up to whole minutes', () => {
    expect(elapsedMinutes(0, 0)).toBe(1);
    expect(elapsedMinutes(0, 61_000)).toBe(2);
  });

  it('explains what blocks finishing', () => {
    const draft = { ...freshDraft(defaults), sets: [set()] };
    expect(english(finishError(draft, 45, true))).toBe('Save or cancel your set edits before finishing.');
    expect(english(finishError({ ...draft, sets: [] }, 45, false))).toBe('Add a set before finishing your workout.');
    expect(english(finishError(draft, 0, false))).toBe('Enter a workout duration of 1–600 minutes in Session settings.');
    expect(finishError(draft, 45, false)).toBeNull();
  });
});

describe('choosing between this device and the server', () => {
  const local = { ...freshDraft(defaults, 1000), savedAt: 2000 };

  it('prefers a copy with sets', () => {
    const server = { ...freshDraft(defaults, 1000), savedAt: 1500, sets: [set()] };
    expect(newerDraft(local, server)).toBe(server);
    expect(newerDraft({ ...local, sets: [set()] }, { ...server, sets: [], savedAt: 9000 })?.sets).toHaveLength(1);
  });

  it('takes the later change and keeps this device on a tie', () => {
    const withSets = { ...local, sets: [set()] };
    const later = { ...withSets, draftId: newDraftId(), savedAt: 3000 };
    expect(newerDraft(withSets, later)).toBe(later);
    expect(newerDraft(withSets, { ...later, savedAt: 2000 })).toBe(withSets);
  });

  it('uses whichever copy exists', () => {
    expect(newerDraft(null, local)).toBe(local);
    expect(newerDraft(local, null)).toBe(local);
    expect(newerDraft(null, null)).toBeNull();
  });
});
