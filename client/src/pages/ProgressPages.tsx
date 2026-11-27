import { useState, type FormEvent, type ReactNode } from 'react';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router';
import { api, FormError } from '../api';
import { ErrorAlert, PageState } from '../components/ui';
import { formatFixed, formatNumber } from '../format';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n, type MessageKey, type Text } from '../i18n/i18n';
import type { CatalogExercise, ExerciseType } from './workouts/draft';

interface ProgressEntry {
  exerciseId: number;
  exerciseName: string;
  muscleGroup: string;
  muscleSubGroup: string;
  exerciseType: ExerciseType;
  hasCurrentResult: boolean;
  initialWeightKg: number;
  initialReps: number;
  initialDurationSeconds: number;
  initialIntensity: number;
  currentWeightKg: number;
  currentReps: number;
  currentDurationSeconds: number;
  currentIntensity: number;
  weightProgressKg: number;
  repsProgressCount: number;
  cardioDurationProgressSeconds: number;
  cardioIntensityProgressCount: number;
}

interface ResultPair {
  initialValue1: number;
  initialValue2: number;
  currentValue1: number;
  currentValue2: number;
}

function signed(value: number, decimals = 1): string {
  const rounded = Number(value.toFixed(decimals));
  if (rounded === 0) return '0';
  return `${rounded > 0 ? '+' : ''}${formatNumber(rounded, decimals)}`;
}

function byMuscleGroup(entries: ProgressEntry[]): [string, ProgressEntry[]][] {
  const groups = new Map<string, ProgressEntry[]>();
  for (const entry of entries) groups.set(entry.muscleGroup, [...(groups.get(entry.muscleGroup) ?? []), entry]);
  return [...groups];
}

function Result({ entry, at }: { entry: ProgressEntry; at: 'initial' | 'current' }) {
  const { t } = useI18n();
  const weight = at === 'initial' ? entry.initialWeightKg : entry.currentWeightKg;
  const reps = at === 'initial' ? entry.initialReps : entry.currentReps;
  const duration = at === 'initial' ? entry.initialDurationSeconds : entry.currentDurationSeconds;
  const intensity = at === 'initial' ? entry.initialIntensity : entry.currentIntensity;
  if (entry.exerciseType === 'StrengthTraining') {
    return (
      <>
        <p className="small mb-1"><strong>{formatNumber(weight)}</strong> {t('unit.kgShort')}</p>
        <p className="small mb-0"><strong>{reps}</strong> {t('word.reps', { count: reps })}</p>
      </>
    );
  }
  return (
    <>
      <p className="small mb-1"><strong>{duration}</strong> {t('unit.secShort')}</p>
      <p className="small mb-0"><strong>{entry.exerciseType === 'Cardio' ? `${intensity}/10` : t('unit.kg', { value: weight })}</strong></p>
    </>
  );
}

function Change({ entry }: { entry: ProgressEntry }) {
  const { t } = useI18n();
  if (!entry.hasCurrentResult) return <p className="small text-muted">{t('progress.addCurrent')}</p>;
  if (entry.exerciseType === 'StrengthTraining') {
    return (
      <>
        <p className="small mb-1">{t('unit.kg', { value: signed(entry.weightProgressKg) })}</p>
        <p className="small mb-0">{t('progress.repsChange', { value: signed(entry.repsProgressCount, 0) })}</p>
      </>
    );
  }
  return (
    <>
      <p className="small mb-1">{t('unit.sec', { value: signed(entry.cardioDurationProgressSeconds, 0) })}</p>
      <p className="small mb-0">
        {entry.exerciseType === 'Cardio'
          ? t('progress.effortChange', { value: signed(entry.cardioIntensityProgressCount, 0) })
          : t('unit.kg', { value: signed(entry.weightProgressKg) })}
      </p>
    </>
  );
}

function ProgressCard({ entry, children }: { entry: ProgressEntry; children?: ReactNode }) {
  const { t } = useI18n();
  return (
    <div className="col-md-6 col-lg-4">
      <div className="card h-100 border border-light d-flex flex-column">
        <div className="card-header bg-dark text-white">
          <strong>{entry.exerciseName}</strong><br />
          <small className="text-muted">{entry.muscleSubGroup}</small>
        </div>
        <div className="card-body d-flex flex-column flex-grow-1">
          <div className="row g-2 mb-3 flex-grow-1">
            <div className="col-4"><div className="border-end ps-0 pe-2">
              <h6 className="text-muted small fw-bold">{t('progress.initial')}</h6>
              <Result entry={entry} at="initial" />
            </div></div>
            <div className="col-4"><div className="text-center border-end ps-1 pe-1">
              <h6 className="text-muted small fw-bold">{t('progress.change')}</h6>
              <Change entry={entry} />
            </div></div>
            <div className="col-4"><div className="ps-2 pe-0">
              <h6 className="text-info small fw-bold">{t('progress.current')}</h6>
              {entry.hasCurrentResult ? <Result entry={entry} at="current" /> : <p className="small text-muted">{t('progress.notRecorded')}</p>}
            </div></div>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}

function ProgressGroups({ entries, controls }: { entries: ProgressEntry[]; controls?: (entry: ProgressEntry) => ReactNode }) {
  return (
    <div className="row">
      {byMuscleGroup(entries).map(([group, items]) => (
        <div className="col-md-12 mb-4" key={group}>
          <h4>{group}</h4>
          <div className="row g-3">
            {items.map((entry) => <ProgressCard key={entry.exerciseId} entry={entry}>{controls?.(entry)}</ProgressCard>)}
          </div>
        </div>
      ))}
    </div>
  );
}

function QuickUpdate({ entry, onSaved }: { entry: ProgressEntry; onSaved: () => void }) {
  const { t } = useI18n();
  const [first, setFirst] = useState('');
  const [second, setSecond] = useState('');
  const save = useAction();
  const strength = entry.exerciseType === 'StrengthTraining';
  const cardio = entry.exerciseType === 'Cardio';

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!first || !second) {
      save.setError(new FormError(t('progress.bothValues')));
      return;
    }
    const [a, b] = [Number(first), Number(second)];
    const body = strength ? { weightKg: a, reps: b } : cardio ? { durationSeconds: a, intensity: b } : { durationSeconds: a, weightKg: b };
    void save.run(async () => {
      await api.put(`/api/progress/${entry.exerciseId}/current`, body);
      setFirst('');
      setSecond('');
      onSaved();
    });
  }

  return (
    <form className="mt-2 mb-3 px-2 border-top pt-2" onSubmit={submit} noValidate>
      <h6 className="small fw-bold">{t('progress.update')}</h6>
      <ErrorAlert error={save.error} />
      <div className="input-group input-group-sm">
        <input type="number" className="form-control" step={strength ? 0.5 : 1}
          aria-label={strength ? t('progress.newWeightLabel') : t('progress.secondsLabel')}
          placeholder={strength ? t('progress.newWeight') : t('progress.seconds')} value={first} onChange={(e) => setFirst(e.target.value)} />
        <input type="number" className="form-control" step={entry.exerciseType === 'Timed' ? 0.5 : 1}
          aria-label={strength ? t('progress.newRepsLabel') : cardio ? t('progress.effortLabel') : t('progress.loadLabel')}
          placeholder={strength ? t('progress.newReps') : cardio ? t('progress.effortLabel') : t('progress.loadShort')}
          value={second} onChange={(e) => setSecond(e.target.value)} />
        <button type="submit" className="btn btn-primary" disabled={save.busy}>{t('progress.setResult')}</button>
      </div>
    </form>
  );
}

export function ProgressPage() {
  const { t } = useI18n();
  usePageTitle(t('progress.title'));
  const navigate = useNavigate();
  const location = useLocation();
  const { data, error, loading, reload } = useApi<ProgressEntry[]>('/api/progress');
  const remove = useAction();

  function deleteEntry(entry: ProgressEntry) {
    if (!window.confirm(t('progress.confirmDelete', { name: entry.exerciseName }))) return;
    void remove.run(async () => {
      await api.delete(`/api/progress/${entry.exerciseId}`);
      navigate(location.pathname, { replace: true, state: { message: t('progress.deleted') } });
      reload();
    });
  }

  return (
    <div className="mt-4">
      <h2 className="mb-4">{t('progress.heading')}</h2>
      <p className="small text-muted">{t('progress.note')}</p>
      <div className="mb-4"><Link to="/progress/track" className="btn btn-primary">{t('progress.add')}</Link></div>
      <ErrorAlert error={remove.error} />
      {!data && <PageState error={error} loading={loading} />}
      {data?.length === 0 && <div className="alert alert-info">{t('progress.empty')}</div>}
      {data && data.length > 0 && (
        <ProgressGroups entries={data} controls={(entry) => (
          <>
            <QuickUpdate entry={entry} onSaved={reload} />
            <div className="row g-2 mt-auto">
              <div className="col-6">
                <Link className="btn btn-sm btn-outline-primary w-100" to={`/progress/track?exerciseId=${entry.exerciseId}`}>{t('common.edit')}</Link>
              </div>
              <div className="col-6">
                <button type="button" className="btn btn-sm btn-outline-danger w-100" disabled={remove.busy} onClick={() => deleteEntry(entry)}>
                  {t('common.delete')}
                </button>
              </div>
            </div>
          </>
        )} />
      )}
    </div>
  );
}

interface Limit {
  min: number;
  max: number;
  step: number;
}

function limits(type: ExerciseType): [Limit, Limit] {
  const strength = type === 'StrengthTraining';
  return [
    { min: strength ? 0 : 1, max: strength ? 500 : 86400, step: strength ? 0.5 : 1 },
    { min: type === 'Timed' ? 0 : 1, max: type === 'Cardio' ? 10 : 500, step: type === 'Timed' ? 0.5 : 1 },
  ];
}

function isValid(text: string, limit: Limit): boolean {
  const value = Number(text);
  return text.trim() !== '' && Number.isFinite(value) && value >= limit.min && value <= limit.max
    && Number.isInteger(Math.round(((value - limit.min) / limit.step) * 1e6) / 1e6);
}

type Values = [string, string, string, string];

/** The saved pair of an exercise as the four form texts; undefined until the answer for this exercise arrives. */
function useSavedResults(exerciseId: number | undefined) {
  const { data, error, loading, isStale, reload } = useApi<ResultPair | undefined>(
    exerciseId === undefined ? null : `/api/progress/${exerciseId}`);
  if (exerciseId === undefined || (loading && !isStale)) return { retry: reload };
  if (error) return { failed: true, retry: reload };
  const values: Values = data
    ? [String(data.initialValue1), String(data.initialValue2), String(data.currentValue1), String(data.currentValue2)]
    : ['', '', '', ''];
  return { values, retry: reload };
}

export function TrackExercisePage() {
  const { t } = useI18n();
  usePageTitle(t('track.title'));
  const [params] = useSearchParams();
  const { data: catalog, error, loading } = useApi<CatalogExercise[]>('/api/progress/catalog');
  if (!catalog) return <PageState error={error} loading={loading} />;
  return <TrackExercise catalog={catalog} initialExerciseId={Number(params.get('exerciseId')) || 0} />;
}

function columnTexts(type: ExerciseType): { labels: [MessageKey, MessageKey]; placeholders: [MessageKey, MessageKey | null] } {
  if (type === 'StrengthTraining') return { labels: ['track.load', 'track.reps'], placeholders: ['unit.kgShort', 'track.reps'] };
  if (type === 'Cardio') return { labels: ['track.durationSeconds', 'track.intensity'], placeholders: ['track.secondsPlaceholder', null] };
  return { labels: ['track.durationSeconds', 'track.externalLoad'], placeholders: ['track.secondsPlaceholder', 'unit.kgShort'] };
}

function TrackExercise({ catalog, initialExerciseId }: { catalog: CatalogExercise[]; initialExerciseId: number }) {
  const { t, show } = useI18n();
  // Ids, not names: names change with the interface language.
  const [selection, setSelection] = useState(() => {
    const chosen = catalog.find((e) => e.exerciseId === initialExerciseId);
    return { groupId: chosen ? String(chosen.muscleGroupId) : '', subGroupId: '', exerciseId: chosen ? String(chosen.exerciseId) : '' };
  });
  const [edited, setEdited] = useState<{ exerciseId: number; values: Values }>();
  const [message, setMessage] = useState<{ text: Text; error: boolean }>();
  const save = useAction();

  const exercise = catalog.find((e) => e.exerciseId === Number(selection.exerciseId));
  const saved = useSavedResults(exercise?.exerciseId);
  const values = exercise && edited?.exerciseId === exercise.exerciseId ? edited.values : saved.values;
  const groups = [...new Map(catalog.map((e) => [e.muscleGroupId, e.muscleGroupName]))]
    .sort((a, b) => a[1].localeCompare(b[1]));
  const groupRows = catalog.filter((e) => e.muscleGroupId === Number(selection.groupId));
  const subGroups = [...new Map(groupRows.map((e) => [e.muscleSubGroupId, e.muscleSubGroupName]))];
  const exerciseRows = groupRows.filter((e) => !selection.subGroupId || e.muscleSubGroupId === Number(selection.subGroupId));

  function choose(next: typeof selection) {
    setSelection(next);
    setEdited(undefined);
    setMessage(undefined);
  }

  function setValue(index: number, text: string) {
    if (!exercise || !values) return;
    const next = [...values] as Values;
    next[index] = text;
    setEdited({ exerciseId: exercise.exerciseId, values: next });
  }

  const type = exercise?.exerciseType ?? 'StrengthTraining';
  const [firstLimit, secondLimit] = limits(type);
  const valid = values !== undefined && values.every((text, i) => isValid(text, i % 2 === 0 ? firstLimit : secondLimit));
  const texts = columnTexts(type);

  let display = t('track.enter');
  if (saved.failed) display = t('track.unavailable');
  else if (!values) display = t('track.loading');
  else if (valid && values) {
    const [i1, i2, c1, c2] = values.map(Number);
    const first = type === 'StrengthTraining'
      ? t('track.kgLoad', { value: signed(c1 - i1) })
      : t('track.secDuration', { value: signed(c1 - i1) });
    const second = type === 'StrengthTraining' ? t('track.repsUnit', { value: signed(c2 - i2) })
      : type === 'Cardio' ? t('track.effortPoints', { value: signed(c2 - i2) }) : t('track.kgLoad', { value: signed(c2 - i2) });
    display = t('track.compare', { first, second });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!exercise || !values) return;
    if (!valid) {
      setMessage({ text: { key: 'track.invalid' }, error: true });
      return;
    }
    const [initialValue1, initialValue2, currentValue1, currentValue2] = values.map(Number);
    void save.run(async () => {
      setMessage(undefined);
      await api.put(`/api/progress/${exercise.exerciseId}`, { initialValue1, initialValue2, currentValue1, currentValue2 });
      setMessage({ text: { key: 'track.saved' }, error: false });
    });
  }

  function column(title: string, border: string, offset: 0 | 2, button?: ReactNode) {
    const prefix = offset === 0 ? 'initial' : 'current';
    const secondPlaceholder = texts.placeholders[1];
    return (
      <div className="col-md-4">
        <div className={`card border-${border}`}>
          <div className={`card-header bg-${border} text-dark`}><h6 className="mb-0">{title}</h6></div>
          <div className="card-body">
            <div className="mb-3">
              <label className="form-label fw-bold" htmlFor={`${prefix}Value1`}>{t(texts.labels[0])}</label>
              <input type="number" className="form-control" id={`${prefix}Value1`} placeholder={t(texts.placeholders[0])}
                min={firstLimit.min} max={firstLimit.max} step={firstLimit.step} disabled={!values || save.busy}
                value={values?.[offset] ?? ''} onChange={(e) => setValue(offset, e.target.value)} />
            </div>
            <div className="mb-3">
              <label className="form-label fw-bold" htmlFor={`${prefix}Value2`}>{t(texts.labels[1])}</label>
              <input type="number" className="form-control" id={`${prefix}Value2`} placeholder={secondPlaceholder ? t(secondPlaceholder) : '1–10'}
                min={secondLimit.min} max={secondLimit.max} step={secondLimit.step} disabled={!values || save.busy}
                value={values?.[offset + 1] ?? ''} onChange={(e) => setValue(offset + 1, e.target.value)} />
            </div>
            {button}
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
      <h2>{t('track.heading')}</h2>
      <p className="small text-muted">{t('progress.note')}</p>
      <div className="mb-4"><Link to="/progress" className="btn btn-outline-light btn-sm">{t('track.back')}</Link></div>

      <div className="card mb-4">
        <div className="card-header">{t('track.select')}</div>
        <div className="card-body"><div className="row g-3">
          <div className="col-md-4">
            <label className="form-label" htmlFor="muscleGroupSelect">{t('track.group')}</label>
            <select className="form-select" id="muscleGroupSelect" value={selection.groupId} disabled={save.busy}
              onChange={(e) => choose({ groupId: e.target.value, subGroupId: '', exerciseId: '' })}>
              <option value="">{t('track.chooseGroup')}</option>
              {groups.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
            </select>
          </div>
          <div className="col-md-4">
            <label className="form-label" htmlFor="muscleSubgroupSelect">{t('track.subgroup')}</label>
            <select className="form-select" id="muscleSubgroupSelect" value={selection.subGroupId} disabled={subGroups.length === 0 || save.busy}
              onChange={(e) => choose({ ...selection, subGroupId: e.target.value, exerciseId: '' })}>
              <option value="">{t('track.allSubgroups')}</option>
              {subGroups.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
            </select>
          </div>
          <div className="col-md-4">
            <label className="form-label" htmlFor="exerciseSelect">{t('common.exercise')}</label>
            <select className="form-select" id="exerciseSelect" value={selection.exerciseId} disabled={exerciseRows.length === 0 || save.busy}
              onChange={(e) => choose({ ...selection, exerciseId: e.target.value })}>
              <option value="">{t('logger.chooseExercise')}</option>
              {exerciseRows.map((e) => <option key={e.exerciseId} value={e.exerciseId}>{e.exerciseName}</option>)}
            </select>
          </div>
        </div></div>
      </div>

      {exercise && (
        <form className="row g-3" onSubmit={submit} noValidate>
          {column(t('track.initial'), 'info', 0)}
          <div className="col-md-4">
            <div className="card border-success">
              <div className="card-header bg-success text-dark"><h6 className="mb-0">{t('track.progress')}</h6></div>
              <div className="card-body text-center" role="status">{display}</div>
            </div>
          </div>
          {column(t('track.current'), 'warning', 2,
            <button type="submit" className="btn btn-primary w-100" disabled={!values || save.busy}>{t('track.setResult')}</button>)}
        </form>
      )}

      {saved.failed && (
        <div className="alert alert-danger mt-4" role="alert">
          {t('track.loadFailed')}
          <button type="button" className="btn btn-outline-secondary btn-sm ms-2" onClick={saved.retry}>{t('common.retry')}</button>
        </div>
      )}
      <div className="mt-4"><ErrorAlert error={save.error} /></div>
      {message && <div className={`alert mt-4 ${message.error ? 'alert-danger' : 'alert-success'}`} role="status">{show(message.text)}</div>}
    </>
  );
}

export function FriendProgressPage() {
  const { t } = useI18n();
  usePageTitle(t('friendProgress.title'));
  const friendId = Number(useParams().friendId);
  const { data, error, loading } = useApi<{ friendUsername: string; currentStrengthCoefficient: number; exercises: ProgressEntry[] }>(
    `/api/friends/${friendId}/progress`);

  return (
    <div className="mt-4">
      {!data ? <PageState error={error} loading={loading} /> : (
        <>
          <h2 className="text-break">{t('friendProgress.heading', { name: data.friendUsername })}</h2>
          <p className="small text-muted">{t('progress.note')}</p>
          <div className="row mb-4"><div className="col-md-6">
            <div className="card border border-light"><div className="card-body">
              <h6 className="text-muted small mb-2">{t('friendProgress.strength')}</h6>
              <h3 className="mb-0">{formatFixed(data.currentStrengthCoefficient, 2)}</h3>
            </div></div>
          </div></div>
          {data.exercises.length === 0
            ? <div className="alert alert-info">{t('friendProgress.empty', { name: data.friendUsername })}</div>
            : <ProgressGroups entries={data.exercises} />}
        </>
      )}
      <div className="mt-4"><Link to="/friends" className="btn btn-outline-light">{t('friends.back')}</Link></div>
    </div>
  );
}
