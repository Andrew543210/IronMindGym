import { useEffect, useMemo, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { flushSync } from 'react-dom';
import { useNavigate } from 'react-router';
import { api } from '../../api';
import { ErrorAlert, PageState } from '../../components/ui';
import { useAction, useApi } from '../../hooks';
import { useI18n, type Text } from '../../i18n/i18n';
import { useAthlete } from '../../session';
import {
  MAX_SETS, applyStored, clockText, draftStore, elapsedMinutes, fieldsFrom, finishError, freshDraft, readSet,
  restoreDraft, setCells, startingFields,
  type CatalogExercise, type DraftSettings, type SetFields, type StoredDraft,
} from './draft';

interface Option {
  id: number;
  name: string;
}

interface SplitDay {
  value: string;
  label: string;
}

export interface LoggerOptions {
  activeSplitTypeName: string | null;
  activeSplitTypeId: number | null;
  splitTypes: (Option & { days: SplitDay[] })[];
  setTypes: Option[];
  defaultSetTypeId: number;
  muscleGroups: Option[];
  exercises: CatalogExercise[];
}

interface PreviousSet {
  weightKg: number;
  reps: number;
  durationSeconds: number;
  intensity: number;
  restSeconds: number;
  setTypeId: number;
  localDate: string;
}

const TRAINING_DAY = 'Training Day';

export function WorkoutLoggerPage() {
  const athlete = useAthlete();
  const { t } = useI18n();
  const { data, error, loading } = useApi<LoggerOptions>('/api/workouts/logger');
  return (
    <>
      <h2>{t('workouts.title')}</h2>
      <p className="mb-3 text-muted">{t('logger.intro')}</p>
      {data ? <WorkoutLogger options={data} userId={athlete.id} /> : <PageState error={error} loading={loading} />}
    </>
  );
}

function dayValues(options: LoggerOptions, splitTypeId: string): string[] {
  const days = options.splitTypes.find((s) => String(s.id) === splitTypeId)?.days ?? [];
  return days.length > 0 ? days.map((day) => day.value) : [TRAINING_DAY];
}

function withValidSplit(draft: StoredDraft, options: LoggerOptions): StoredDraft {
  const split = options.splitTypes.find((s) => String(s.id) === draft.settings.splitTypeId) ?? options.splitTypes[0];
  const splitTypeId = split ? String(split.id) : '';
  const days = dayValues(options, splitTypeId);
  const day = days.includes(draft.settings.splitDayLabel) ? draft.settings.splitDayLabel : days[0];
  return { ...draft, settings: { ...draft.settings, splitTypeId: splitTypeId, splitDayLabel: day } };
}

function storageWorks(): boolean {
  try {
    localStorage.setItem('ironmind-storage-check', '1');
    localStorage.removeItem('ironmind-storage-check');
    return true;
  } catch {
    return false;
  }
}

/** Loads the athlete's last saved set of an exercise. Each answer is kept with its exercise id, so a late answer is ignored. */
function usePreviousSet(exerciseId: number | undefined) {
  const [result, setResult] = useState<{ exerciseId: number; value?: PreviousSet | null; failed?: boolean }>();
  useEffect(() => {
    if (exerciseId === undefined) return;
    let active = true;
    api.get<PreviousSet | undefined>(`/api/workouts/previous-set?exerciseId=${exerciseId}`).then(
      (value) => active && setResult({ exerciseId, value: value ?? null }),
      () => active && setResult({ exerciseId, failed: true }),
    );
    return () => {
      active = false;
    };
  }, [exerciseId]);
  return result?.exerciseId === exerciseId ? result : undefined;
}

function WorkoutLogger({ options, userId }: { options: LoggerOptions; userId: number }) {
  const navigate = useNavigate();
  const { t, show } = useI18n();
  const catalog = options.exercises;
  const store = useMemo(() => draftStore(userId), [userId]);
  const defaults = useMemo<DraftSettings>(() => {
    const splitTypeId = String(options.activeSplitTypeId ?? options.splitTypes[0]?.id ?? '');
    return { splitTypeId: splitTypeId, splitDayLabel: dayValues(options, splitTypeId)[0], durationMinutes: '60', reviewRating: '4', reviewComment: '' };
  }, [options]);

  const [draft, setDraft] = useState(() => withValidSplit(restoreDraft(store.read(), defaults), options));
  // Status and errors are kept as text keys, so they follow a change of the interface language.
  const [status, setStatus] = useState<Text>(() => (draft.sets.length
    ? { key: 'logger.restored', params: { count: draft.sets.length } }
    : { key: 'logger.draftSaved' }));
  const [canStore] = useState(storageWorks);
  const [now, setNow] = useState(() => Date.now());

  const [selection, setSelection] = useState(() => {
    const last = catalog.find((e) => e.exerciseId === draft.sets.at(-1)?.exerciseId);
    return { groupId: last ? String(last.muscleGroupId) : '', subGroupId: '', exerciseId: last ? String(last.exerciseId) : '' };
  });
  const [fields, setFields] = useState<SetFields>(() =>
    startingFields(draft.sets, Number(selection.exerciseId) || undefined, options.defaultSetTypeId));
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [query, setQuery] = useState('');
  const [error, setError] = useState<Text | null>(null);
  const save = useAction();

  const saved = useRef(false);
  const changedElsewhere = useRef(false);
  const restAlerted = useRef(draft.timerEnd <= now);
  const editorRef = useRef<HTMLDivElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);
  const exerciseRef = useRef<HTMLSelectElement>(null);
  const weightRef = useRef<HTMLInputElement>(null);
  const durationRef = useRef<HTMLInputElement>(null);

  const item = catalog.find((e) => e.exerciseId === Number(selection.exerciseId));
  const type = item?.exerciseType ?? 'StrengthTraining';
  const previous = usePreviousSet(item?.exerciseId);

  const groupRows = catalog.filter((e) => e.muscleGroupId === Number(selection.groupId));
  const subGroups = [...new Map(groupRows.map((e) => [e.muscleSubGroupId, e.muscleSubGroupName]))];
  const exerciseRows = groupRows.filter((e) => !selection.subGroupId || e.muscleSubGroupId === Number(selection.subGroupId));
  const search = query.trim().toLocaleLowerCase();
  const matches = search
    ? catalog.filter((e) => e.exerciseName.toLocaleLowerCase().includes(search) || e.englishName.toLowerCase().includes(search)).slice(0, 8)
    : [];
  const setTypeName = (id: number | string) => options.setTypes.find((s) => String(s.id) === String(id))?.name;
  const splitDays = options.splitTypes.find((s) => String(s.id) === draft.settings.splitTypeId)?.days ?? [];
  const dayOptions = splitDays.length > 0 ? splitDays : [{ value: TRAINING_DAY, label: t('logger.trainingDay') }];

  // Both clocks are derived from timestamps, so a throttled background tab shows the right values when it returns.
  const remaining = Math.max(0, Math.ceil((draft.timerEnd - now) / 1000));
  const resting = draft.timerEnd > 0 && remaining > 0;
  const restDone = draft.timerEnd > 0 && !resting;
  const elapsed = Math.max(0, Math.floor((now - draft.startedAt) / 1000));
  const durationMinutes = draft.autoDuration ? elapsedMinutes(draft.startedAt, now) : Number(draft.settings.durationMinutes);

  useEffect(() => {
    if (saved.current) return;
    if (changedElsewhere.current) {
      changedElsewhere.current = false;
      return;
    }
    store.write(draft);
  }, [draft, store]);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 500);
    const onVisible = () => {
      if (!document.hidden) setNow(Date.now());
    };
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, []);

  const title = resting ? t('logger.restTitle', { time: clockText(remaining) }) : t('workouts.title');
  useEffect(() => {
    document.title = `${title} · IronMind`;
  }, [title]);

  useEffect(() => {
    if (!restDone || restAlerted.current) return;
    restAlerted.current = true;
    navigator.vibrate?.([200, 100, 200]);
  }, [restDone]);

  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key !== store.key) return;
      const stored = store.read();
      changedElsewhere.current = true;
      setEditingIndex(null);
      setDraft((current) => (stored
        ? withValidSplit(applyStored(stored, current), options)
        : { ...freshDraft(current.settings), autoDuration: current.autoDuration }));
      setNow(Date.now());
      setStatus({ key: 'logger.updatedElsewhere' });
    }
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, [store, options]);

  useEffect(() => {
    if (draft.sets.length === 0) return;
    function onBeforeUnload(event: BeforeUnloadEvent) {
      if (saved.current) return;
      event.preventDefault();
      event.returnValue = '';
    }
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => window.removeEventListener('beforeunload', onBeforeUnload);
  }, [draft.sets.length]);

  function update(next: StoredDraft, message: Text = { key: 'logger.draftSaved' }) {
    setDraft(next);
    setStatus(message);
  }

  function selectGroup(groupId: string) {
    setSelection({ groupId, subGroupId: '', exerciseId: '' });
    setFields(startingFields(draft.sets, undefined, options.defaultSetTypeId));
  }

  function selectSubGroup(subGroupId: string) {
    setSelection({ ...selection, subGroupId, exerciseId: '' });
    setFields(startingFields(draft.sets, undefined, options.defaultSetTypeId));
  }

  function selectExercise(exerciseId: string) {
    setSelection({ ...selection, exerciseId });
    setFields(startingFields(draft.sets, Number(exerciseId) || undefined, options.defaultSetTypeId));
  }

  function chooseExercise(exercise: CatalogExercise) {
    setSelection({ groupId: String(exercise.muscleGroupId), subGroupId: '', exerciseId: String(exercise.exerciseId) });
    setFields(startingFields(draft.sets, exercise.exerciseId, options.defaultSetTypeId));
  }

  function pickSearchResult(exercise: CatalogExercise) {
    flushSync(() => {
      chooseExercise(exercise);
      setQuery('');
    });
    exerciseRef.current?.focus();
  }

  function searchKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return;
    event.preventDefault();
    if (matches[0]) pickSearchResult(matches[0]);
  }

  function setField(name: keyof SetFields, value: string) {
    setFields({ ...fields, [name]: value });
  }

  function endEdit() {
    setEditingIndex(null);
  }

  function editSet(index: number) {
    const set = draft.sets[index];
    const exercise = catalog.find((e) => e.exerciseId === set.exerciseId);
    if (!exercise) {
      setError({ key: 'logger.unavailableExercise' });
      return;
    }
    flushSync(() => {
      setEditingIndex(index);
      setSelection({ groupId: String(exercise.muscleGroupId), subGroupId: '', exerciseId: String(exercise.exerciseId) });
      setFields(fieldsFrom(set, options.defaultSetTypeId));
    });
    editorRef.current?.scrollIntoView?.({ behavior: 'smooth', block: 'start' });
    (exercise.exerciseType === 'StrengthTraining' ? weightRef : durationRef).current?.focus({ preventScroll: true });
  }

  function removeSet(index: number) {
    const set = draft.sets[index];
    const name = catalog.find((e) => e.exerciseId === set.exerciseId)?.exerciseName || t('common.exercise');
    if (!window.confirm(t('logger.confirmRemove', { number: index + 1, name }))) return;
    endEdit();
    update({ ...draft, sets: draft.sets.filter((_, i) => i !== index) }, { key: 'logger.setRemoved' });
  }

  function addSet() {
    const { set, error: problem } = readSet(item, fields, setTypeName(fields.setTypeId));
    if (problem !== undefined) {
      setError({ key: problem });
      return;
    }
    if (editingIndex === null && draft.sets.length >= MAX_SETS) {
      setError({ key: 'logger.maxSets', params: { max: MAX_SETS } });
      return;
    }
    setError(null);
    endEdit();
    if (editingIndex !== null) {
      update({ ...draft, sets: draft.sets.map((s, i) => (i === editingIndex ? set : s)) }, { key: 'logger.setUpdated' });
      return;
    }
    restAlerted.current = set.restSeconds === 0;
    const started = Date.now();
    setNow(started);
    update({ ...draft, sets: [...draft.sets, set], timerEnd: started + set.restSeconds * 1000 }, { key: 'logger.setLogged' });
  }

  function startTimer() {
    const seconds = Number(fields.restSeconds);
    if (!Number.isInteger(seconds) || seconds < 0 || seconds > 3600) return;
    restAlerted.current = seconds === 0;
    const started = Date.now();
    setNow(started);
    update({ ...draft, timerEnd: started + seconds * 1000 });
  }

  function skipRest() {
    restAlerted.current = true;
    const skipped = Date.now();
    setNow(skipped);
    update({ ...draft, timerEnd: skipped });
  }

  function discardWorkout() {
    const count = draft.sets.length;
    if (count && !window.confirm(t('logger.confirmDiscard', { count }))) return;
    store.clear();
    restAlerted.current = true;
    endEdit();
    setError(null);
    setNow(Date.now());
    update({ ...freshDraft(draft.settings), autoDuration: draft.autoDuration }, { key: 'logger.newWorkout' });
  }

  function setSetting(name: keyof DraftSettings, value: string) {
    const settings = { ...draft.settings, [name]: value };
    if (name === 'splitTypeId') settings.splitDayLabel = dayValues(options, value)[0];
    update({ ...draft, settings, autoDuration: name === 'durationMinutes' ? false : draft.autoDuration });
  }

  function setAutoDuration(on: boolean) {
    const settings = on ? draft.settings : { ...draft.settings, durationMinutes: String(durationMinutes) };
    update({ ...draft, autoDuration: on, settings });
  }

  function finish(event: FormEvent) {
    event.preventDefault();
    const problem = finishError(draft, durationMinutes, editingIndex !== null);
    if (problem) {
      flushSync(() => setError({ key: problem }));
      errorRef.current?.scrollIntoView?.({ block: 'center' });
      return;
    }
    void save.run(async () => {
      const result = await api.post<{ id: number }>('/api/workouts', {
        splitTypeId: draft.settings.splitTypeId ? Number(draft.settings.splitTypeId) : null,
        splitDayLabel: draft.settings.splitDayLabel,
        durationMinutes,
        draftId: draft.draftId,
        reviewRating: Number(draft.settings.reviewRating),
        reviewComment: draft.settings.reviewComment,
        sets: draft.sets,
      });
      saved.current = true;
      store.clear();
      navigate(`/workouts/${result.id}`, { state: { message: t('logger.saved') } });
    });
  }

  function previousText(): string {
    if (!item) return t('logger.previousChoose');
    if (!previous) return t('logger.previousLoading');
    if (previous.failed) return t('logger.previousFailed');
    const last = previous.value;
    if (!last) return t('logger.previousNone');
    const value = type === 'StrengthTraining'
      ? t('logger.previousStrength', { weight: last.weightKg, reps: t('count.reps', { count: last.reps }) })
      : type === 'Cardio'
        ? t('logger.previousCardio', { seconds: last.durationSeconds, effort: last.intensity })
        : t('logger.previousTimed', { seconds: last.durationSeconds, weight: last.weightKg });
    return t('logger.previous', { date: last.localDate, value });
  }
  const previousValue = previous?.value;

  return (
    <form className="row g-3 mb-4" noValidate onSubmit={finish}>
      <div className="col-12">
        <div className="workout-toolbar d-flex justify-content-between align-items-center gap-3">
          <span>{t('logger.sessionTime')} <strong className="metric">{clockText(elapsed)}</strong></span>
          <span className="small text-muted" role="status">{canStore ? show(status) : t('logger.storageUnavailable')}</span>
        </div>
      </div>

      <div className="col-12">
        <div className="card border-secondary">
          <details>
            <summary className="card-header">{t('logger.settings')}{options.activeSplitTypeName ? ` · ${options.activeSplitTypeName}` : ''}</summary>
            <div className="card-body row g-3">
              <div className="col-md-4">
                <label htmlFor="splitType" className="form-label">{t('setup.splitType')}</label>
                <select id="splitType" className="form-select" value={draft.settings.splitTypeId}
                  disabled={options.activeSplitTypeId !== null} onChange={(e) => setSetting('splitTypeId', e.target.value)}>
                  {options.splitTypes.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
              <div className="col-md-4">
                <label htmlFor="splitDay" className="form-label">{t('logger.splitDay')}</label>
                <select id="splitDay" className="form-select" value={draft.settings.splitDayLabel}
                  onChange={(e) => setSetting('splitDayLabel', e.target.value)}>
                  {dayOptions.map((day) => <option key={day.value} value={day.value}>{day.label}</option>)}
                </select>
              </div>
              <div className="col-md-4">
                <label htmlFor="durationMinutes" className="form-label">{t('logger.durationMinutes')}</label>
                <input id="durationMinutes" type="number" className="form-control" min={1} max={600} inputMode="numeric"
                  value={draft.autoDuration ? String(durationMinutes) : draft.settings.durationMinutes}
                  onChange={(e) => setSetting('durationMinutes', e.target.value)} />
                <label className="small mt-2">
                  <input type="checkbox" checked={draft.autoDuration} onChange={(e) => setAutoDuration(e.target.checked)} /> {t('logger.useElapsed')}
                </label>
              </div>
            </div>
          </details>
        </div>
      </div>

      <div className="col-12">
        <div className="card border-secondary">
          <div className="card-header">{t('logger.addHeader')}</div>
          <div className="card-body row g-3" ref={editorRef}>
            <div className="col-12">
              <label htmlFor="exerciseSearch" className="form-label">{t('logger.find')}</label>
              <input type="search" id="exerciseSearch" className="form-control" placeholder={t('logger.searchPlaceholder')}
                autoComplete="off" value={query} onChange={(e) => setQuery(e.target.value)} onKeyDown={searchKeyDown} />
              <div className="exercise-search-results" aria-live="polite">
                {search && matches.length === 0 && t('logger.noMatches')}
                {matches.map((match) => (
                  <button type="button" key={match.exerciseId} className="exercise-search-result" onClick={() => pickSearchResult(match)}>
                    {match.exerciseName} · {match.muscleGroupName}
                  </button>
                ))}
              </div>
            </div>
            <div className="col-12">
              <details className="exercise-browser">
                <summary>{t('logger.browse')}</summary>
                <div className="row g-3 mt-1">
                  <div className="col-md-6">
                    <label htmlFor="muscleGroupFilter" className="form-label">{t('logger.muscleGroup')}</label>
                    <select id="muscleGroupFilter" className="form-select" value={selection.groupId} onChange={(e) => selectGroup(e.target.value)}>
                      <option value="">{t('logger.selectGroup')}</option>
                      {options.muscleGroups.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}
                    </select>
                  </div>
                  <div className="col-md-6">
                    <label htmlFor="muscleSubGroupFilter" className="form-label">{t('logger.focusArea')}</label>
                    <select id="muscleSubGroupFilter" className="form-select" value={selection.subGroupId}
                      disabled={subGroups.length === 0} onChange={(e) => selectSubGroup(e.target.value)}>
                      <option value="">{t('logger.allFocusAreas')}</option>
                      {subGroups.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
                    </select>
                  </div>
                </div>
              </details>
            </div>
            <div className="col-12">
              <label htmlFor="exerciseSelect" className="form-label">{t('common.exercise')}</label>
              <select id="exerciseSelect" ref={exerciseRef} className="form-select" value={selection.exerciseId}
                disabled={exerciseRows.length === 0} onChange={(e) => selectExercise(e.target.value)}>
                <option value="">{t('logger.chooseExercise')}</option>
                {exerciseRows.map((e) => <option key={e.exerciseId} value={e.exerciseId}>{e.exerciseName}</option>)}
              </select>
            </div>

            <div className="col-12">
              <div className="previous-set">
                <span className="small text-muted" role="status">{previousText()}</span>
                {previousValue && (
                  <button type="button" className="btn btn-outline-secondary btn-sm"
                    onClick={() => setFields(fieldsFrom(previousValue, options.defaultSetTypeId))}>{t('logger.useValues')}</button>
                )}
              </div>
            </div>
            <div className="col-md-3">
              <label htmlFor="setTypeSelect" className="form-label">{t('logger.setType')}</label>
              <select id="setTypeSelect" className="form-select" value={fields.setTypeId} onChange={(e) => setField('setTypeId', e.target.value)}>
                {options.setTypes.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            {type !== 'Cardio' && (
              <div className="col-md-3">
                <label htmlFor="weightInput" className="form-label">{t('logger.load')}</label>
                <input id="weightInput" ref={weightRef} className="form-control" type="number" inputMode="decimal" min={0} max={500} step={0.5}
                  value={fields.weightKg} onChange={(e) => setField('weightKg', e.target.value)} />
              </div>
            )}
            {type === 'StrengthTraining' && (
              <div className="col-md-3">
                <label htmlFor="repsInput" className="form-label">{t('logger.reps')}</label>
                <input id="repsInput" className="form-control" type="number" inputMode="numeric" min={1} max={500}
                  value={fields.reps} onChange={(e) => setField('reps', e.target.value)} />
              </div>
            )}
            {type !== 'StrengthTraining' && (
              <div className="col-md-3">
                <label htmlFor="durationInput" className="form-label">{t('logger.durationSeconds')}</label>
                <input id="durationInput" ref={durationRef} className="form-control" type="number" inputMode="numeric" min={1} max={86400}
                  value={fields.durationSeconds} onChange={(e) => setField('durationSeconds', e.target.value)} />
              </div>
            )}
            {type === 'Cardio' && (
              <div className="col-md-3">
                <label htmlFor="intensityInput" className="form-label">{t('logger.effort')}</label>
                <input id="intensityInput" className="form-control" type="number" inputMode="numeric" min={1} max={10}
                  value={fields.intensity} onChange={(e) => setField('intensity', e.target.value)} />
              </div>
            )}
            <div className="col-md-3">
              <label htmlFor="restInput" className="form-label">{t('logger.rest')}</label>
              <input id="restInput" className="form-control" type="number" inputMode="numeric" min={0} max={3600}
                value={fields.restSeconds} onChange={(e) => setField('restSeconds', e.target.value)} />
            </div>

            <div className="col-12">
              <div ref={errorRef} className="alert alert-danger" role="alert" hidden={!error}>{error ? show(error) : ''}</div>
              <div className="d-flex gap-2">
                <button type="button" className="btn btn-primary flex-grow-1" onClick={addSet}>
                  {editingIndex === null ? t('logger.logSet') : t('logger.saveSet', { number: editingIndex + 1 })}
                </button>
                {editingIndex !== null && <button type="button" className="btn btn-outline-secondary" onClick={endEdit}>{t('logger.cancelEdit')}</button>}
              </div>
              <details className="small text-muted mt-2">
                <summary>{t('logger.howTo')}</summary>
                <p className="mt-2 mb-0">{t('logger.howToText')}</p>
              </details>
            </div>

            <div className="col-12">
              <div className={`workout-toolbar d-flex flex-wrap gap-3 align-items-center justify-content-between${restDone ? ' rest-done' : ''}`}>
                <span>{t('logger.restTimer')} <strong className="metric">{clockText(remaining)}</strong></span>
                <div>
                  <button type="button" className="btn btn-outline-secondary btn-sm" onClick={startTimer}>{t('logger.startTimer')}</button>{' '}
                  <button type="button" className="btn btn-outline-warning btn-sm" onClick={skipRest}>{t('logger.skipRest')}</button>
                </div>
              </div>
              <div className="mt-2" role="status">{restDone ? t('logger.restDone') : ''}</div>
            </div>

            <div className="col-12">
              <div className="section-heading">
                <h5 className="mb-0">{t('logger.thisSession')}</h5>
                <span className="small text-muted">{t('logger.logged', { sets: t('count.sets', { count: draft.sets.length }) })}</span>
              </div>
              {draft.sets.length === 0 && <p className="text-muted small">{t('logger.firstSet')}</p>}
              <div className="table-responsive">
                <table className="table table-sm">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>{t('common.exercise')}</th>
                      <th>{t('workout.type')}</th>
                      <th>{t('logger.loadOrDuration')}</th>
                      <th>{t('logger.repsOrEffort')}</th>
                      <th>{t('workout.rest')}</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {draft.sets.map((set, index) => (
                      <tr key={index}>
                        <td>{index + 1}</td>
                        {setCells(set, catalog.find((e) => e.exerciseId === set.exerciseId), t, setTypeName(set.setTypeId))
                          .map((cell, i) => <td key={i}>{cell}</td>)}
                        <td className="text-nowrap">
                          <button type="button" className="btn btn-sm btn-outline-secondary" aria-label={t('logger.editSet', { number: index + 1 })}
                            onClick={() => editSet(index)}>{t('common.edit')}</button>{' '}
                          <button type="button" className="btn btn-sm btn-outline-danger" aria-label={t('logger.removeSet', { number: index + 1 })}
                            onClick={() => removeSet(index)}>×</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="col-md-2">
        <label htmlFor="reviewRating" className="form-label">{t('logger.rating')}</label>
        <input id="reviewRating" type="number" className="form-control" min={1} max={5} inputMode="numeric"
          value={draft.settings.reviewRating} onChange={(e) => setSetting('reviewRating', e.target.value)} />
      </div>
      <div className="col-md-10">
        <label htmlFor="reviewComment" className="form-label">{t('logger.review')}</label>
        <input id="reviewComment" className="form-control" maxLength={800} placeholder={t('logger.reviewPlaceholder')}
          value={draft.settings.reviewComment} onChange={(e) => setSetting('reviewComment', e.target.value)} />
      </div>

      <div className="col-12 d-grid gap-2">
        <ErrorAlert error={save.error} />
        <button type="submit" className="btn btn-success" disabled={save.busy}>{save.busy ? t('logger.finishing') : t('logger.finish')}</button>
        <button type="button" className="btn btn-outline-danger" disabled={save.busy} onClick={discardWorkout}>{t('logger.discard')}</button>
      </div>
    </form>
  );
}
