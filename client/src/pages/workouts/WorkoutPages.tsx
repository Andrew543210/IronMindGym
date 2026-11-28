import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import { api } from '../../api/client';
import type { WorkoutDetails, WorkoutHistory } from '../../api/types/workouts';
import { Phrase } from '../../components/Phrase';
import { ErrorAlert, Glyph, PageState } from '../../components/ui';
import { flash } from '../../flash';
import { localDateTime } from '../../format';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useAthlete } from '../../session';
import { draftStore, newerDraft, validSets } from './draft';
import { SERVER_DRAFT_ROUTE, type ServerDraft } from './serverDraft';
import { WorkoutReactions, WorkoutSessionView } from './WorkoutView';

export function WorkoutHistoryPage() {
  const { t } = useI18n();
  usePageTitle(t('workouts.title'));
  const athlete = useAthlete();
  const [params] = useSearchParams();
  const page = Math.max(1, Math.floor(Number(params.get('page'))) || 1);
  const { data, error, loading, isStale } = useApi<WorkoutHistory>(`/api/workouts?page=${page}`, { keepPrevious: true });
  // While the next page loads, the previous one stays on screen and the pager belongs to it.
  const shownPage = data?.page ?? page;
  const [localDraft] = useState(() => draftStore(athlete.id).read());
  const { data: serverDraft } = useApi<ServerDraft | undefined>(SERVER_DRAFT_ROUTE);
  const unsavedSets = validSets(newerDraft(localDraft, serverDraft?.content ?? null)?.sets).length;

  return (
    <>
      <h1 className="h2">{t('workouts.title')}</h1>
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

      <h2 className="h4">{t('workouts.history')}</h2>
      {!data && <PageState error={error} loading={loading} />}
      {isStale && <p className="small text-muted" role="status">{t('common.loading')}</p>}
      {data?.sessions.length === 0 && (
        <p className="text-muted">{shownPage > 1 ? t('workouts.noOlder') : t('workouts.none')}</p>
      )}
      <ul className="list-group mb-4" aria-busy={isStale}>
        {data?.sessions.map((s) => (
          <li key={s.id} className="list-group-item d-flex justify-content-between align-items-start align-items-sm-center gap-2">
            <span>
              <Phrase text="workouts.entry" parts={{
                date: <span className="metric">{localDateTime(s.sessionDate, athlete.timeZone)}</span>,
                name: s.name,
                duration: <span className="metric">{t('unit.min', { value: s.durationMinutes })}</span>,
              }} />
            </span>
            <Link className="btn btn-outline-primary btn-sm" to={`/workouts/${s.id}`}>{t('common.details')}</Link>
          </li>
        ))}
      </ul>

      <div className="d-flex gap-2 mb-4">
        {shownPage > 1 && <Link className="btn btn-outline-secondary btn-sm" to={shownPage === 2 ? '/workouts' : `/workouts?page=${shownPage - 1}`}><Glyph>←</Glyph> {t('workouts.newer')}</Link>}
        {data?.hasOlderSessions && <Link className="btn btn-outline-secondary btn-sm" to={`/workouts?page=${shownPage + 1}`}>{t('workouts.older')} <Glyph>→</Glyph></Link>}
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
      void navigate('/workouts', { state: flash({ key: 'workoutDetails.deleted' }) });
    });
  }

  return (
    <>
      <h1 className="h2">{t('workoutDetails.title')}</h1>
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
