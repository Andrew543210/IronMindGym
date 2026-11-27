import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { api } from '../../api/client';
import type { ProfileSettings, SettingsPage } from '../../api/types/profile';
import { ErrorAlert, PageState } from '../../components/ui';
import { deviceTimeZone } from '../../format';
import { textFields } from '../../forms';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useSession } from '../../session';

type SettingsForm = Record<keyof ProfileSettings, string>;

const optionalNumbers = ['calorieTarget', 'proteinTarget', 'fatTarget', 'carbsTarget'] as const;

function toForm(settings: ProfileSettings): SettingsForm {
  return Object.fromEntries(Object.entries(settings).map(([key, value]) => [key, value === null ? '' : String(value)])) as SettingsForm;
}

export function ProfileSettingsPage() {
  const { t } = useI18n();
  usePageTitle(t('settings.title'));
  const { refresh } = useSession();
  const navigate = useNavigate();
  const { data, error: loadError, loading } = useApi<SettingsPage>('/api/profile/settings');
  const [edited, setEdited] = useState<SettingsForm>();
  const { busy, error, run } = useAction();
  if (!data) return <PageState error={loadError} loading={loading} />;
  const form = edited ?? toForm(data.settings);

  const field = textFields(form, setEdited);

  function submit(event: FormEvent) {
    event.preventDefault();
    void run(async () => {
      const optional = Object.fromEntries(optionalNumbers.map((name) => [name, form[name] === '' ? null : Number(form[name])]));
      await api.put('/api/profile/settings', {
        username: form.username.trim(),
        age: Number(form.age),
        timeZone: form.timeZone.trim() || null,
        weightKg: Number(form.weightKg),
        heightCm: Number(form.heightCm),
        benchPressKg: Number(form.benchPressKg),
        squatKg: Number(form.squatKg),
        deadliftKg: Number(form.deadliftKg),
        workoutsPerWeek: Number(form.workoutsPerWeek),
        splitTypeId: Number(form.splitTypeId),
        energyEquation: form.energyEquation,
        ...optional,
      });
      await refresh();
      navigate('/profile', { state: { message: t('settings.saved') } });
    });
  }

  const number = (name: keyof ProfileSettings, label: string, min: number, max: number, step = 1, className = 'col-6') => (
    <div className={className}>
      <label htmlFor={name} className="form-label">{label}</label>
      <input {...field(name)} type="number" inputMode={step < 1 ? 'decimal' : 'numeric'} min={min} max={max} step={step}
        required={!(optionalNumbers as readonly string[]).includes(name)} className="form-control" />
    </div>
  );

  return (
    <>
      <p className="eyebrow">{t('settings.eyebrow')}</p>
      <h2>{t('settings.title')}</h2>
      <form className="row g-4" onSubmit={submit}>
        <div className="col-12"><ErrorAlert error={error} /></div>
        <div className="col-lg-6">
          <div className="card h-100"><div className="card-body">
            <h4>{t('settings.about')}</h4>
            <div className="mb-3">
              <label htmlFor="username" className="form-label">{t('common.username')}</label>
              <input {...field('username')} className="form-control" required minLength={2} maxLength={50} pattern="[A-Za-z0-9_.\-]{2,50}" autoComplete="username" />
            </div>
            <div className="row g-3 mb-3">{number('age', t('profile.age'), 12, 100)}</div>
            <div className="mb-3">
              <label htmlFor="timeZone" className="form-label">{t('settings.timeZone')}</label>
              <div className="input-group">
                <input {...field('timeZone')} className="form-control" placeholder={t('settings.timeZonePlaceholder')} maxLength={64}
                  autoComplete="off" autoCapitalize="off" spellCheck={false} aria-describedby="timeZoneHint" />
                <button type="button" className="btn btn-outline-secondary" onClick={() => setEdited({ ...form, timeZone: deviceTimeZone() })}>
                  {t('settings.useDevice')}
                </button>
              </div>
              <p className="form-text small text-muted mb-0" id="timeZoneHint">{t('settings.timeZoneHint')}</p>
            </div>
            <div className="row g-3">
              {number('weightKg', t('field.weightKg'), 30, 250, 0.1)}
              {number('heightCm', t('field.heightCm'), 120, 230, 0.1)}
            </div>
          </div></div>
        </div>
        <div className="col-lg-6">
          <div className="card h-100"><div className="card-body">
            <h4>{t('settings.training')}</h4>
            <div className="mb-3">
              <label htmlFor="splitTypeId" className="form-label">{t('settings.split')}</label>
              <select {...field('splitTypeId')} className="form-select" required>
                {data.splitTypes.map((split) => <option key={split.id} value={split.id}>{split.name}</option>)}
              </select>
            </div>
            <div className="row g-3 mb-3">{number('workoutsPerWeek', t('settings.sessions'), 1, 7)}</div>
            <p className="text-muted small">{t('settings.liftsHint')}</p>
            <div className="row g-3">
              {number('benchPressKg', t('settings.bench'), 0, 500, 0.5, 'col-4')}
              {number('squatKg', t('settings.squat'), 0, 500, 0.5, 'col-4')}
              {number('deadliftKg', t('settings.deadlift'), 0, 500, 0.5, 'col-4')}
            </div>
          </div></div>
        </div>
        <div className="col-12">
          <div className="card"><div className="card-body">
            <h4>{t('settings.nutrition')}</h4>
            <p className="text-muted small">{t('settings.nutritionHint')}</p>
            <div className="row g-3">
              <div className="col-md-4">
                <label htmlFor="energyEquation" className="form-label">{t('settings.equation')}</label>
                <select {...field('energyEquation')} className="form-select">
                  <option value="None">{t('settings.equationNone')}</option>
                  <option value="Male">{t('settings.equationMale')}</option>
                  <option value="Female">{t('settings.equationFemale')}</option>
                </select>
              </div>
              {number('calorieTarget', t('field.caloriesKcal'), 500, 10000, 1, 'col-6 col-md-2')}
              {number('proteinTarget', t('field.proteinG'), 1, 1000, 1, 'col-6 col-md-2')}
              {number('fatTarget', t('field.fatG'), 1, 1000, 1, 'col-6 col-md-2')}
              {number('carbsTarget', t('field.carbsG'), 1, 2000, 1, 'col-6 col-md-2')}
            </div>
          </div></div>
        </div>
        <div className="col-12 d-flex gap-2">
          <button className="btn btn-primary" type="submit" disabled={busy}>{busy ? t('common.saving') : t('settings.submit')}</button>
          <Link className="btn btn-outline-secondary" to="/profile">{t('settings.cancel')}</Link>
        </div>
      </form>
    </>
  );
}
