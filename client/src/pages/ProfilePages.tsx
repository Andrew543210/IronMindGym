import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { api, FormError } from '../api';
import { ErrorAlert, PageState, ResponsiveTable } from '../components/ui';
import { calendarDay, deviceTimeZone, formatDay, formatFixed, localDay } from '../format';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { useAthlete, useSession } from '../session';

interface Profile {
  username: string;
  email: string;
  age: number;
  avatarName: string;
  strengthCoefficient: number;
  weightKg: number | null;
  heightCm: number | null;
  measuredAt: string | null;
  benchPressKg: number | null;
  squatKg: number | null;
  deadliftKg: number | null;
  weightHistory: { date: string; valueKg: number }[];
  achievements: { name: string; conditionDescription: string; achievedAt: string }[];
}

interface Settings {
  username: string;
  age: number;
  timeZone: string | null;
  weightKg: number;
  heightCm: number;
  benchPressKg: number;
  squatKg: number;
  deadliftKg: number;
  workoutsPerWeek: number;
  splitTypeId: number;
  energyEquation: 'None' | 'Male' | 'Female';
  calorieTarget: number | null;
  proteinTarget: number | null;
  fatTarget: number | null;
  carbsTarget: number | null;
}

type SettingsForm = Record<keyof Settings, string>;

const optionalNumbers = ['calorieTarget', 'proteinTarget', 'fatTarget', 'carbsTarget'] as const;

export function ProfilePage() {
  const { t } = useI18n();
  usePageTitle(t('profile.title'));
  const athlete = useAthlete();
  const { data, error, loading, reload } = useApi<Profile>('/api/profile');
  if (!data) return <PageState error={error} loading={loading} />;

  const kg = (value: number | null) => (value === null ? '—' : t('unit.kg', { value: formatFixed(value, 1) }));
  const facts: [string, string][] = [
    [t('common.username'), data.username],
    [t('common.email'), data.email],
    [t('profile.age'), String(data.age)],
    [t('profile.rank'), data.avatarName],
    [t('profile.strength'), formatFixed(data.strengthCoefficient, 2)],
  ];
  const body: [string, string][] = [
    [t('profile.weight'), kg(data.weightKg)],
    [t('profile.height'), data.heightCm === null ? '—' : t('unit.cm', { value: formatFixed(data.heightCm, 1) })],
    [t('profile.measured'), data.measuredAt ? formatDay(localDay(data.measuredAt, athlete.timeZone)) : '—'],
    [t('profile.bench'), kg(data.benchPressKg)],
    [t('profile.squat'), kg(data.squatKg)],
    [t('profile.deadlift'), kg(data.deadliftKg)],
  ];

  return (
    <>
      <div className="section-heading mt-0">
        <div>
          <p className="eyebrow">{t('profile.eyebrow')}</p>
          <h2 className="mb-0">{t('profile.heading')}</h2>
        </div>
        <Link to="/profile/settings" className="btn btn-primary">{t('profile.edit')}</Link>
      </div>
      <div className="d-flex flex-wrap gap-2 mb-4">
        <Link className="btn btn-outline-secondary btn-sm" to="/profile/password">{t('profile.changePassword')}</Link>
        <a className="btn btn-outline-secondary btn-sm" href="/api/profile/export" download>{t('profile.export')}</a>
      </div>
      <div className="row g-3 mb-4">
        <div className="col-md-6">
          <div className="card h-100"><div className="card-body">
            <dl className="mb-0">
              {[...facts, ...body].map(([label, value]) => (
                <div key={label} className="d-flex justify-content-between gap-3 py-1">
                  <dt className="text-muted fw-normal">{label}</dt>
                  <dd className="mb-0 metric text-end">{value}</dd>
                </div>
              ))}
            </dl>
            <Link to="/progress" className="btn btn-primary btn-sm mt-3">{t('profile.viewProgress')}</Link>
          </div></div>
        </div>
        <div className="col-md-6">
          <div className="card h-100"><div className="card-body">
            <div className="section-heading mt-0">
              <h5 className="mb-0">{t('profile.awards')}</h5>
              <Link className="small text-muted" to="/awards">{t('profile.allAwards')}</Link>
            </div>
            {data.achievements.length === 0
              ? <p className="text-muted mb-0">{t('profile.noAwards')}</p>
              : (
                <ul className="list-unstyled mb-0">
                  {data.achievements.map((award) => (
                    <li key={award.name} className="mb-2">
                      <strong>{award.name}</strong>
                      <div className="small text-muted">{award.conditionDescription} · {formatDay(localDay(award.achievedAt, athlete.timeZone))}</div>
                    </li>
                  ))}
                </ul>
              )}
          </div></div>
        </div>
      </div>
      <div className="row g-4">
        <div className="col-lg-5"><WeightForm onSaved={reload} /></div>
        <div className="col-lg-7"><WeightHistory entries={data.weightHistory} /></div>
      </div>
    </>
  );
}

function WeightForm({ onSaved }: { onSaved: () => void }) {
  const { t } = useI18n();
  const athlete = useAthlete();
  const [form, setForm] = useState({ weightKg: '', date: athlete.today });
  const [saved, setSaved] = useState<string>();
  const { busy, error, run } = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    setSaved(undefined);
    void run(async () => {
      await api.put(`/api/profile/weights/${form.date}`, { weightKg: Number(form.weightKg) });
      setSaved(t('profile.weightSaved', { date: formatDay(form.date) }));
      setForm({ ...form, weightKg: '' });
      onSaved();
    });
  }

  return (
    <div className="card h-100"><div className="card-body">
      <p className="eyebrow">{t('profile.logEyebrow')}</p>
      <h4>{t('profile.logHeading')}</h4>
      <p className="text-muted small">{t('profile.logHint')}</p>
      <form className="row g-3" onSubmit={submit}>
        <ErrorAlert error={error} />
        {saved && <div className="alert alert-success" role="status">{saved}</div>}
        <div className="col-6">
          <label htmlFor="weightKg" className="form-label">{t('field.weightKg')}</label>
          <input id="weightKg" type="number" inputMode="decimal" step="0.1" min={30} max={250} required className="form-control"
            value={form.weightKg} onChange={(e) => setForm({ ...form, weightKg: e.target.value })} />
        </div>
        <div className="col-6">
          <label htmlFor="weightDate" className="form-label">{t('common.date')}</label>
          <input id="weightDate" type="date" required max={athlete.today} className="form-control"
            value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} />
        </div>
        <div className="col-12">
          <button className="btn btn-primary" type="submit" disabled={busy}>{busy ? t('common.saving') : t('profile.saveWeight')}</button>
        </div>
      </form>
    </div></div>
  );
}

function WeightHistory({ entries }: { entries: Profile['weightHistory'] }) {
  const { t } = useI18n();
  const ordered = [...entries].sort((a, b) => a.date.localeCompare(b.date));
  const minimum = Math.min(...ordered.map((e) => e.valueKg)) - 2;
  const range = Math.max(4, Math.max(...ordered.map((e) => e.valueKg)) - minimum);
  const short = { month: 'short', day: 'numeric' } as const;

  return (
    <div className="card h-100"><div className="card-body">
      <h4>{t('profile.history')}</h4>
      {ordered.length === 0 ? <p className="text-muted mb-0">{t('profile.historyEmpty')}</p> : (
        <>
          <div className="weight-bars" role="img" aria-label={t('profile.historyChart', { count: ordered.length })}>
            {ordered.map((entry) => (
              <div key={entry.date} className="weight-bar" style={{ height: `${(15 + ((entry.valueKg - minimum) / range) * 85).toFixed(1)}%` }}
                title={`${formatDay(entry.date, short)}: ${t('unit.kg', { value: formatFixed(entry.valueKg, 1) })}`}></div>
            ))}
          </div>
          <div className="d-flex justify-content-between small text-muted mt-2">
            <span>{formatDay(ordered[0].date, short)}</span>
            <span>{formatDay(ordered[ordered.length - 1].date, short)}</span>
          </div>
          <details className="mt-3">
            <summary>{t('profile.entries')}</summary>
            <ResponsiveTable>
              <thead><tr><th>{t('common.date')}</th><th>{t('profile.weight')}</th></tr></thead>
              <tbody>
                {[...ordered].reverse().map((entry) => (
                  <tr key={entry.date}><td className="metric">{calendarDay(entry.date)}</td><td className="metric">{t('unit.kg', { value: formatFixed(entry.valueKg, 1) })}</td></tr>
                ))}
              </tbody>
            </ResponsiveTable>
          </details>
        </>
      )}
    </div></div>
  );
}

function toForm(settings: Settings): SettingsForm {
  return Object.fromEntries(Object.entries(settings).map(([key, value]) => [key, value === null ? '' : String(value)])) as SettingsForm;
}

export function ProfileSettingsPage() {
  const { t } = useI18n();
  usePageTitle(t('settings.title'));
  const { refresh } = useSession();
  const navigate = useNavigate();
  const { data, error: loadError, loading } = useApi<{ settings: Settings; splitTypes: { id: number; name: string }[] }>('/api/profile/settings');
  const [edited, setEdited] = useState<SettingsForm>();
  const { busy, error, run } = useAction();
  if (!data) return <PageState error={loadError} loading={loading} />;
  const form = edited ?? toForm(data.settings);

  const field = (name: keyof Settings) => ({
    id: name,
    value: form[name],
    onChange: (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setEdited({ ...form, [name]: e.target.value }),
  });

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

  const number = (name: keyof Settings, label: string, min: number, max: number, step = 1, className = 'col-6') => (
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

export function ChangePasswordPage() {
  const { t } = useI18n();
  usePageTitle(t('password.title'));
  const { signedOut } = useSession();
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const { busy, error, run, setError } = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    if (form.newPassword !== form.confirmPassword) {
      setError(new FormError(t('register.mismatch')));
      return;
    }
    void run(async () => {
      await api.put('/api/profile/password', form);
      signedOut('passwordChanged');
    });
  }

  const input = (name: keyof typeof form, label: string, autoComplete: string) => (
    <div className="col-12">
      <label htmlFor={name} className="form-label">{label}</label>
      <input id={name} type="password" className="form-control" required minLength={name === 'currentPassword' ? undefined : 10} maxLength={128}
        autoComplete={autoComplete} value={form[name]} onChange={(e) => setForm({ ...form, [name]: e.target.value })} />
    </div>
  );

  return (
    <>
      <p className="eyebrow">{t('password.eyebrow')}</p>
      <h2>{t('password.title')}</h2>
      <div className="card narrow-card"><div className="card-body">
        <p className="text-muted small">{t('password.hint')}</p>
        <form className="row g-3" onSubmit={submit}>
          <ErrorAlert error={error} />
          {input('currentPassword', t('password.current'), 'current-password')}
          {input('newPassword', t('password.new'), 'new-password')}
          {input('confirmPassword', t('password.confirm'), 'new-password')}
          <div className="col-12">
            <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t('common.saving') : t('password.submit')}</button>
          </div>
        </form>
      </div></div>
    </>
  );
}
