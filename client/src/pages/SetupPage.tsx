import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { api } from '../api/client';
import type { SetupForm } from '../api/types/profile';
import { ErrorAlert, PageState } from '../components/ui';
import { flash } from '../flash';
import { deviceTimeZone } from '../format';
import { textFields } from '../forms';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n, type MessageKey } from '../i18n/i18n';
import { useSession } from '../session';

const lifts: { name: 'benchPressKg' | 'squatKg' | 'deadliftKg'; label: MessageKey }[] = [
  { name: 'benchPressKg', label: 'setup.bench' },
  { name: 'squatKg', label: 'setup.squat' },
  { name: 'deadliftKg', label: 'setup.deadlift' },
];

export function SetupPage() {
  const { t } = useI18n();
  usePageTitle(t('setup.title'));
  const { refresh } = useSession();
  const navigate = useNavigate();
  const { data, error: loadError, loading } = useApi<SetupForm>('/api/setup');
  const [form, setForm] = useState({
    weightKg: '', heightCm: '', benchPressKg: '0', squatKg: '0', deadliftKg: '0', workoutsPerWeek: '3', splitTypeId: '',
  });
  const { busy, error, run } = useAction();

  if (!data) return <PageState error={loadError} loading={loading} />;
  if (data.alreadyCompleted) return <Navigate to="/" replace />;
  const splitTypeId = form.splitTypeId || String(data.splitTypes[0]?.id ?? '');

  const field = textFields(form, setForm);

  function submit(event: FormEvent) {
    event.preventDefault();
    void run(async () => {
      await api.post('/api/setup', {
        weightKg: Number(form.weightKg),
        heightCm: Number(form.heightCm),
        benchPressKg: Number(form.benchPressKg),
        squatKg: Number(form.squatKg),
        deadliftKg: Number(form.deadliftKg),
        workoutsPerWeek: Number(form.workoutsPerWeek),
        splitTypeId: Number(splitTypeId),
        timeZone: deviceTimeZone() || null,
      });
      await refresh();
      navigate('/', { replace: true, state: flash({ key: 'setup.saved' }) });
    });
  }

  return (
    <>
      <h1 className="h2">{t('setup.title')}</h1>
      <p className="text-muted">{t('setup.intro')}</p>
      <form className="row g-3" onSubmit={submit}>
        <ErrorAlert error={error} />
        <div className="col-6 col-md-3">
          <label htmlFor="weightKg" className="form-label">{t('setup.weight')}</label>
          <input {...field('weightKg')} type="number" inputMode="decimal" step="0.1" min={30} max={250} required className="form-control" />
        </div>
        <div className="col-6 col-md-3">
          <label htmlFor="heightCm" className="form-label">{t('setup.height')}</label>
          <input {...field('heightCm')} type="number" inputMode="decimal" step="0.1" min={120} max={230} required className="form-control" />
        </div>
        <div className="col-12">
          <p className="form-text mb-0" id="liftHint">{t('setup.liftHint')}</p>
        </div>
        {lifts.map((lift) => (
          <div className="col-12 col-sm-4 col-md-2" key={lift.name}>
            <label htmlFor={lift.name} className="form-label">{t(lift.label)}</label>
            <input {...field(lift.name)} type="number" inputMode="decimal" step="0.5" min={0} max={500} required className="form-control" aria-describedby="liftHint" />
          </div>
        ))}
        <div className="col-6 col-md-4">
          <label htmlFor="workoutsPerWeek" className="form-label">{t('setup.workoutsPerWeek')}</label>
          <input {...field('workoutsPerWeek')} type="number" inputMode="numeric" min={1} max={7} required className="form-control" />
        </div>
        <div className="col-12 col-md-8">
          <label htmlFor="splitTypeId" className="form-label">{t('setup.splitType')}</label>
          <select {...field('splitTypeId')} value={splitTypeId} className="form-select" required>
            {data.splitTypes.map((split) => <option key={split.id} value={split.id}>{split.name}</option>)}
          </select>
        </div>
        <div className="col-12">
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t('common.saving') : t('common.save')}</button>
        </div>
      </form>
    </>
  );
}
