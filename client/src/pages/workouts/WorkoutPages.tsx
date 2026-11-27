import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import { api } from '../../api';
import { ErrorAlert, PageState } from '../../components/ui';
import { localDateTime } from '../../format';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useAthlete } from '../../session';
import type { WorkoutListItem } from '../DashboardPage';
import { draftStore, newerDraft, validSets, type StoredValue } from './draft';
import { readServerDraft } from './serverDraft';
import { WorkoutReactions, WorkoutSessionView, type WorkoutDetails } from './WorkoutView';

interface WorkoutHistory {
  sessions: WorkoutListItem[];
  page: number;
  hasOlderSessions: boolean;
}

export function WorkoutHistoryPage() {
  const { t } = useI18n();
  usePageTitle(t('workouts.title'));
  const athlete = useAthlete();
  const [params] = useSearchParams();
  const page = Math.max(1, Math.floor(Number(params.get('page'))) || 1);
  const { data, error, loading } = useApi<WorkoutHistory>(`/api/workouts?page=${page}`, { keepPrevious: true });
  const [localDraft] = useState(() => draftStore(athlete.id).read());
  const [serverDraft, setServerDraft] = useState<StoredValue | null>(null);
  useEffect(() => {
    readServerDraft().then(setServerDraft, () => undefined);
  }, []);
  const unsavedSets = validSets(newerDraft(localDraft, serverDraft)?.sets).length;

  return (
    <>
      <h2>{t('workouts.title')}</h2>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <p className="mb-0 text-muted">{t('workouts.intro')}</p>
        <Link className="btn btn-primary" to="/workouts/new">{t('workouts.start')}</Link>
      </div>

      {unsavedSets > 0 && (
        <div className="alert alert-warning d-flex flex-wrap align-items-center justify-content-between gap-2" role="status">
          <span>{t('workouts.unsaved', { sets: t('count.sets', { count: unsavedSets }) })}</span>
          <Link className="btn btn-primary btn-sm" to="/workouts/new">{t('workouts.resume')}</Link>
        </div>
      )}

      <h4>{t('workouts.history')}</h4>
      {!data && <PageState error={error} loading={loading} />}
      {data?.sessions.length === 0 && (
        <p className="text-muted">{page > 1 ? t('workouts.noOlder') : t('workouts.none')}</p>
      )}
      <ul className="list-group mb-4">
        {data?.sessions.map((s) => (
          <li key={s.id} className="list-group-item d-flex justify-content-between align-items-center gap-2">
            <span>
              <span className="metric">{localDateTime(s.sessionDate, athlete.timeZone)}</span> - {s.name} (<span className="metric">{t('unit.min', { value: s.durationMinutes })}</span>)
            </span>
            <Link className="btn btn-outline-primary btn-sm" to={`/workouts/${s.id}`}>{t('common.details')}</Link>
          </li>
        ))}
      </ul>

      <div className="d-flex gap-2 mb-4">
        {page > 1 && <Link className="btn btn-outline-secondary btn-sm" to={page === 2 ? '/workouts' : `/workouts?page=${page - 1}`}>{t('workouts.newer')}</Link>}
        {data?.hasOlderSessions && <Link className="btn btn-outline-secondary btn-sm" to={`/workouts?page=${page + 1}`}>{t('workouts.older')}</Link>}
      </div>
    </>
  );
}

export function WorkoutDetailsPage() {
  const { t } = useI18n();
  usePageTitle(t('workoutDetails.title'));
  const athlete = useAthlete();
  const navigate = useNavigate();
  const id = Number(useParams().id);
  const { data, error, loading, reload } = useApi<WorkoutDetails>(`/api/workouts/${id}`);
  const remove = useAction();

  function deleteWorkout() {
    if (!window.confirm(t('workoutDetails.confirmDelete'))) return;
    void remove.run(async () => {
      await api.delete(`/api/workouts/${id}`);
      navigate('/workouts', { state: { message: t('workoutDetails.deleted') } });
    });
  }

  return (
    <>
      <h2>{t('workoutDetails.title')}</h2>
      {!data ? <PageState error={error} loading={loading} /> : (
        <>
          <WorkoutSessionView workout={data} timeZone={athlete.timeZone} />
          <WorkoutReactions workout={data} timeZone={athlete.timeZone} onComment={async (text) => {
            await api.post(`/api/workouts/${id}/comments`, { text });
            reload();
          }} />
          <div className="mt-4">
            <ErrorAlert error={remove.error} />
            <button className="btn btn-outline-danger" type="button" disabled={remove.busy} onClick={deleteWorkout}>{t('workoutDetails.delete')}</button>
          </div>
        </>
      )}
    </>
  );
}
