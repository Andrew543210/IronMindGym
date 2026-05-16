/** The unsaved workout lives in the browser, so a closed tab or a lost connection does not lose sets. */

import type { MessageKey, Translate } from '../../i18n/i18n';

export type ExerciseType = 'StrengthTraining' | 'Cardio' | 'Timed';

export interface CatalogExercise {
  exerciseId: number;
  exerciseName: string;
  englishName: string;
  exerciseType: ExerciseType;
  muscleGroupId: number;
  muscleGroupName: string;
  muscleSubGroupId: number;
  muscleSubGroupName: string;
}

export interface DraftSet {
  exerciseId: number;
  setTypeId: number;
  setTypeName: string;
  weightKg: number;
  reps: number;
  durationSeconds: number;
  intensity: number;
  restSeconds: number;
}

/** Form values as typed, so a half-typed number survives a reload. */
export interface DraftSettings {
  splitTypeId: string;
  splitDayLabel: string;
  durationMinutes: string;
  reviewRating: string;
  reviewComment: string;
}

export interface StoredDraft {
  draftId: string;
  sets: DraftSet[];
  timerEnd: number;
  startedAt: number;
  autoDuration: boolean;
  settings: DraftSettings;
}

export type StoredValue = Partial<Omit<StoredDraft, 'settings'>> & { settings?: Partial<Record<keyof DraftSettings, unknown>> };

export const EMPTY_DRAFT_LIFETIME_MS = 2 * 60 * 60 * 1000;
export const MAX_SETS = 200;

export interface DraftStore {
  key: string;
  read(): StoredValue | null;
  write(state: StoredDraft): boolean;
  clear(): void;
}

/** The draft is kept in localStorage per athlete, so it survives a reload, a closed tab or a tab the phone discarded. */
export function draftStore(userId: number): DraftStore {
  const key = `ironmind-workout-${userId}`;
  return {
    key,
    read() {
      try {
        const raw = localStorage.getItem(key);
        const value: unknown = raw ? JSON.parse(raw) : null;
        return value && typeof value === 'object' ? (value as StoredValue) : null;
      } catch {
        return null;
      }
    },
    write(state) {
      try {
        const raw = JSON.stringify(state);
        // Writing the same text again would only wake other tabs for nothing.
        if (localStorage.getItem(key) !== raw) localStorage.setItem(key, raw);
        return true;
      } catch {
        return false;
      }
    },
    clear() {
      try {
        localStorage.removeItem(key);
      } catch {
        /* Storage may be unavailable. */
      }
    },
  };
}

export function validSets(value: unknown): DraftSet[] {
  return Array.isArray(value)
    ? value.filter((set): set is DraftSet => !!set && Number.isInteger(Number((set as DraftSet).exerciseId)))
    : [];
}

export function newDraftId(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = [...bytes].map((b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

export function isDraftId(value: unknown): value is string {
  return typeof value === 'string' && /^[0-9a-f-]{36}$/i.test(value);
}

export function freshDraft(defaults: DraftSettings, now = Date.now()): StoredDraft {
  return { draftId: newDraftId(), sets: [], timerEnd: 0, startedAt: now, autoDuration: true, settings: { ...defaults } };
}

/**
 * Takes over what another tab or an earlier visit stored. Settings missing in the stored draft keep
 * the defaults, and a start time in the future (a wrong device clock) is ignored.
 */
export function applyStored(stored: StoredValue, base: StoredDraft, now = Date.now()): StoredDraft {
  const startedAt = Number(stored.startedAt);
  const settings = { ...base.settings };
  for (const name of Object.keys(settings) as (keyof DraftSettings)[]) {
    const value = stored.settings?.[name];
    if (value !== null && value !== undefined) settings[name] = String(value);
  }
  return {
    draftId: isDraftId(stored.draftId) ? stored.draftId : base.draftId,
    sets: validSets(stored.sets),
    timerEnd: Number(stored.timerEnd) || 0,
    startedAt: Number.isFinite(startedAt) && startedAt <= now ? startedAt : base.startedAt,
    autoDuration: stored.autoDuration !== false,
    settings,
  };
}

export function restoreDraft(stored: StoredValue | null, defaults: DraftSettings, now = Date.now()): StoredDraft {
  const base = freshDraft(defaults, now);
  if (!stored) return base;
  const recentEmpty = now - Number(stored.startedAt) < EMPTY_DRAFT_LIFETIME_MS;
  return validSets(stored.sets).length > 0 || recentEmpty ? applyStored(stored, base, now) : base;
}

export interface SetFields {
  setTypeId: string;
  weightKg: string;
  reps: string;
  durationSeconds: string;
  intensity: string;
  restSeconds: string;
}

export function fieldsFrom(set: Partial<Omit<DraftSet, 'setTypeId'>> & { setTypeId?: number | string }, defaultSetTypeId: number): SetFields {
  return {
    setTypeId: String(set.setTypeId ?? defaultSetTypeId),
    weightKg: String(set.weightKg ?? 0),
    reps: String(set.reps || 8),
    durationSeconds: String(set.durationSeconds || 60),
    intensity: String(set.intensity || 5),
    restSeconds: String(set.restSeconds ?? 120),
  };
}

export function startingFields(sets: DraftSet[], exerciseId: number | undefined, defaultSetTypeId: number): SetFields {
  const last = sets.findLast((s) => s.exerciseId === exerciseId);
  return fieldsFrom(last ?? {}, defaultSetTypeId);
}

export function readSet(
  item: CatalogExercise | undefined,
  fields: SetFields,
  setTypeName: string | undefined,
): { set: DraftSet; error?: undefined } | { set?: undefined; error: MessageKey } {
  if (!item || !fields.setTypeId || setTypeName === undefined) return { error: 'draft.chooseFirst' };
  const type = item.exerciseType;
  const weightText = fields.weightKg.trim();
  const weight = type === 'Cardio' ? 0 : Number(weightText);
  const reps = type === 'StrengthTraining' ? Number(fields.reps) : 0;
  const duration = type === 'StrengthTraining' ? 0 : Number(fields.durationSeconds);
  const intensity = type === 'Cardio' ? Number(fields.intensity) : 0;
  const rest = Number(fields.restSeconds);
  if (type !== 'Cardio' && (!weightText || !Number.isFinite(weight) || weight < 0 || weight > 500))
    return { error: 'draft.load' };
  if (type === 'StrengthTraining' && (!Number.isInteger(reps) || reps < 1 || reps > 500))
    return { error: 'draft.reps' };
  if (type !== 'StrengthTraining' && (!Number.isInteger(duration) || duration < 1 || duration > 86400))
    return { error: 'draft.duration' };
  if (type === 'Cardio' && (!Number.isInteger(intensity) || intensity < 1 || intensity > 10))
    return { error: 'draft.effort' };
  if (!fields.restSeconds.trim() || !Number.isInteger(rest) || rest < 0 || rest > 3600)
    return { error: 'draft.rest' };
  return {
    set: {
      exerciseId: item.exerciseId,
      setTypeId: Number(fields.setTypeId),
      setTypeName,
      weightKg: weight,
      reps,
      durationSeconds: duration,
      intensity,
      restSeconds: rest,
    },
  };
}

export function setCells(set: DraftSet, item: CatalogExercise | undefined, t: Translate, setTypeName = set.setTypeName): string[] {
  const type = item?.exerciseType ?? 'StrengthTraining';
  const second = type === 'Cardio' ? t('draft.effortCell', { effort: set.intensity })
    : type === 'Timed' ? t('unit.kg', { value: set.weightKg }) : t('count.reps', { count: set.reps });
  return [
    item?.exerciseName || t('common.exercise'),
    setTypeName,
    type === 'StrengthTraining' ? t('unit.kg', { value: set.weightKg }) : t('unit.sec', { value: set.durationSeconds }),
    second,
    t('unit.sec', { value: set.restSeconds }),
  ];
}

export function clockText(seconds: number): string {
  return `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
}

export function elapsedMinutes(startedAt: number, now: number): number {
  return Math.max(1, Math.ceil(Math.max(0, Math.floor((now - startedAt) / 1000)) / 60));
}

export function finishError(draft: StoredDraft, durationMinutes: number, editing: boolean): MessageKey | null {
  if (editing) return 'draft.editing';
  if (draft.sets.length === 0) return 'draft.noSets';
  if (!Number.isInteger(durationMinutes) || durationMinutes < 1 || durationMinutes > 600) return 'draft.durationMinutes';
  if (!draft.settings.splitDayLabel) return 'draft.day';
  return null;
}
