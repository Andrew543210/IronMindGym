import { useEffect, useRef, useState, type FormEvent } from 'react';
import { flushSync } from 'react-dom';
import { useNavigate } from 'react-router';
import { api } from '../../api/client';
import type { CatalogExercise } from '../../api/types/exercises';
import type { LoggerOptions } from '../../api/types/workouts';
import { choiceOf, chosenExercise, type ExerciseChoice } from '../../components/exerciseChoice';
import { ExercisePicker } from '../../components/ExercisePicker';
import { ErrorAlert, PageState } from '../../components/ui';
import { flash } from '../../flash';
import { useAction, useApi } from '../../hooks';
import { useI18n, type Text } from '../../i18n/i18n';
import { useAthlete } from '../../session';
import { MAX_SETS, elapsedMinutes, fieldsFrom, finishError, readSet, startingFields, type SetFields, type StoredValue } from './draft';
import { RestTimer, SessionClock } from './logger/Clocks';
import { ExerciseSearch } from './logger/ExerciseSearch';
import { SessionSets } from './logger/SessionSets';
import { SessionSettings } from './logger/SessionSettings';
import { SetEditor } from './logger/SetEditor';
import { useWorkoutDraft } from './logger/useWorkoutDraft';
import { readServerDraft } from './serverDraft';

export function WorkoutLoggerPage() {
  const athlete = useAthlete();
  const { t } = useI18n();
  const { data, error, loading } = useApi<LoggerOptions>('/api/workouts/logger');
  const [serverCopy, setServerCopy] = useState<{ value: StoredValue | null; known: boolean }>();
  useEffect(() => {
    let active = true;
    readServerDraft().then(
      (value) => active && setServerCopy({ value, known: true }),
      () => active && setServerCopy({ value: null, known: false }),
    );
    return () => {
      active = false;
    };
  }, []);
  return (
    <>
      <h1 className="h2">{t('workouts.title')}</h1>
      <p className="mb-3 text-muted">{t('logger.intro')}</p>
      {data && serverCopy
        ? <WorkoutLogger options={data} userId={athlete.id} serverCopy={serverCopy.value} syncWithServer={serverCopy.known} />
        : <PageState error={error} loading={loading || !serverCopy} />}
    </>
  );
}

interface WorkoutLoggerProps {
  options: LoggerOptions;
  userId: number;
  serverCopy: StoredValue | null;
  syncWithServer: boolean;
}

function WorkoutLogger({ options, userId, serverCopy, syncWithServer }: WorkoutLoggerProps) {
  const navigate = useNavigate();
  const { t, show } = useI18n();
  const catalog = options.exercises;
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  // Errors are kept as text keys, so they follow a change of the interface language.
  const [error, setError] = useState<Text | null>(null);
  const { draft, status, canStore, update, discard, submit } = useWorkoutDraft({
    options, userId, serverCopy, syncWithServer,
    onReplaced: () => {
      setEditingIndex(null);
      setError(null);
    },
  });
  const [choice, setChoice] = useState(() => choiceOf(catalog.find((e) => e.exerciseId === draft.sets.at(-1)?.exerciseId)));
  const [fields, setFields] = useState<SetFields>(() =>
    startingFields(draft.sets, Number(choice.exerciseId) || undefined, options.defaultSetTypeId));
  const save = useAction();

  const editorRef = useRef<HTMLDivElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);
  const exerciseRef = useRef<HTMLSelectElement>(null);
  const weightRef = useRef<HTMLInputElement>(null);
  const durationRef = useRef<HTMLInputElement>(null);

  const exercise = chosenExercise(catalog, choice);
  const setTypeName = (id: number | string) => options.setTypes.find((s) => String(s.id) === String(id))?.name;

  function choose(next: ExerciseChoice) {
    setChoice(next);
    setFields(startingFields(draft.sets, Number(next.exerciseId) || undefined, options.defaultSetTypeId));
  }

  function pickSearchResult(found: CatalogExercise) {
    flushSync(() => choose(choiceOf(found)));
    exerciseRef.current?.focus();
  }

  function endEdit() {
    setEditingIndex(null);
  }

  function editSet(index: number) {
    const set = draft.sets[index];
    if (!set) return;
    const logged = catalog.find((e) => e.exerciseId === set.exerciseId);
    if (!logged) {
      setError({ key: 'logger.unavailableExercise' });
      return;
    }
    flushSync(() => {
      setEditingIndex(index);
      setChoice(choiceOf(logged));
      setFields(fieldsFrom(set, options.defaultSetTypeId));
    });
    editorRef.current?.scrollIntoView?.({ behavior: 'smooth', block: 'start' });
    (logged.exerciseType === 'StrengthTraining' ? weightRef : durationRef).current?.focus({ preventScroll: true });
  }

  function removeSet(index: number) {
    const set = draft.sets[index];
    if (!set) return;
    const name = catalog.find((e) => e.exerciseId === set.exerciseId)?.exerciseName || t('common.exercise');
    if (!window.confirm(t('logger.confirmRemove', { number: index + 1, name }))) return;
    endEdit();
    update({ ...draft, sets: draft.sets.filter((_, i) => i !== index) }, { key: 'logger.setRemoved' });
  }

  function saveSet() {
    const { set, error: problem } = readSet(exercise, fields, setTypeName(fields.setTypeId));
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
    update({ ...draft, sets: [...draft.sets, set], timerEnd: Date.now() + set.restSeconds * 1000 }, { key: 'logger.setLogged' });
  }

  function startTimer() {
    const seconds = Number(fields.restSeconds);
    if (!Number.isInteger(seconds) || seconds < 0 || seconds > 3600) return;
    update({ ...draft, timerEnd: Date.now() + seconds * 1000 });
  }

  function skipRest() {
    update({ ...draft, timerEnd: Date.now() });
  }

  function discardWorkout() {
    const count = draft.sets.length;
    if (count && !window.confirm(t('logger.confirmDiscard', { count }))) return;
    endEdit();
    setError(null);
    discard();
  }

  function finish(event: FormEvent) {
    event.preventDefault();
    const durationMinutes = draft.autoDuration ? elapsedMinutes(draft.startedAt, Date.now()) : Number(draft.settings.durationMinutes);
    const problem = finishError(draft, durationMinutes, editingIndex !== null);
    if (problem) {
      flushSync(() => setError({ key: problem }));
      errorRef.current?.scrollIntoView?.({ block: 'center' });
      return;
    }
    void save.run(async () => {
      const result = await submit(() => api.post<{ id: number }>('/api/workouts', {
        splitDayId: draft.settings.splitDayId ? Number(draft.settings.splitDayId) : null,
        durationMinutes,
        draftId: draft.draftId,
        reviewRating: Number(draft.settings.reviewRating),
        reviewComment: draft.settings.reviewComment,
        sets: draft.sets,
      }));
      void navigate(`/workouts/${result.id}`, { state: flash({ key: 'logger.saved' }) });
    });
  }

  return (
    <form className="row g-3 mb-4 workout-logger" noValidate onSubmit={finish}>
      <div className="col-12">
        <div className="workout-toolbar d-flex justify-content-between align-items-center gap-3">
          <span>{t('logger.sessionTime')} <SessionClock startedAt={draft.startedAt} /></span>
          <span className="small text-muted draft-status" role="status">{canStore ? show(status) : t('logger.storageUnavailable')}</span>
        </div>
      </div>

      <SessionSettings options={options} draft={draft} onChange={(next) => update(next)} />

      <div className="col-12">
        <div className="card">
          <div className="card-header">{t('logger.addHeader')}</div>
          <div className="card-body row g-3 set-editor" ref={editorRef}>
            <ExerciseSearch catalog={catalog} onPick={pickSearchResult} />
            <ExercisePicker catalog={catalog} choice={choice} onChoose={choose} foldFiltersUnder={t('logger.browse')} exerciseRef={exerciseRef} />
            <SetEditor options={options} exercise={exercise} fields={fields} onFields={setFields} editingIndex={editingIndex} error={error}
              onSave={saveSet} onCancel={endEdit} weightRef={weightRef} durationRef={durationRef} errorRef={errorRef} />
            <RestTimer key={draft.timerEnd} timerEnd={draft.timerEnd} onStart={startTimer} onSkip={skipRest} />
            <SessionSets sets={draft.sets} catalog={catalog} setTypeName={setTypeName} onEdit={editSet} onRemove={removeSet} />
          </div>
        </div>
      </div>

      <div className="col-md-2">
        <label htmlFor="reviewRating" className="form-label">{t('logger.rating')}</label>
        <input id="reviewRating" type="number" className="form-control" min={1} max={5} inputMode="numeric"
          value={draft.settings.reviewRating} onChange={(e) => update({ ...draft, settings: { ...draft.settings, reviewRating: e.target.value } })} />
      </div>
      <div className="col-md-10">
        <label htmlFor="reviewComment" className="form-label">{t('logger.review')}</label>
        <input id="reviewComment" className="form-control" maxLength={800} placeholder={t('logger.reviewPlaceholder')}
          value={draft.settings.reviewComment} onChange={(e) => update({ ...draft, settings: { ...draft.settings, reviewComment: e.target.value } })} />
      </div>

      <div className="col-12 d-grid gap-2">
        <ErrorAlert error={save.error} />
        <button type="submit" className="btn btn-success finish-workout" disabled={save.busy}>{save.busy ? t('logger.finishing') : t('logger.finish')}</button>
        <button type="button" className="btn btn-outline-danger" disabled={save.busy} onClick={discardWorkout}>{t('logger.discard')}</button>
      </div>
    </form>
  );
}
