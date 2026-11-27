import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router';
import { api, FormError } from '../../api/client';
import type { ProgressEntry } from '../../api/types/progress';
import { ErrorAlert, PageState } from '../../components/ui';
import { flash } from '../../flash';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { ProgressGroups } from './ProgressCards';

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
      navigate(location.pathname, { replace: true, state: flash({ key: 'progress.deleted' }) });
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
