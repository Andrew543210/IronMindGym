import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { api } from '../../api/client';
import type { Profile } from '../../api/types/profile';
import { ErrorAlert, PageState, ResponsiveTable } from '../../components/ui';
import { formatDay, formatFixed, localDay } from '../../format';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useAthlete } from '../../session';

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
    [t('profile.weighedOn'), data.weighedOn ? formatDay(data.weighedOn) : '—'],
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
    const sent = form;
    void run(async () => {
      await api.put(`/api/profile/weights/${sent.date}`, { weightKg: Number(sent.weightKg) });
      setSaved(t('profile.weightSaved', { date: formatDay(sent.date) }));
      setForm((current) => (current.weightKg === sent.weightKg ? { ...current, weightKg: '' } : current));
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
                  <tr key={entry.date}><td className="metric">{entry.date}</td><td className="metric">{t('unit.kg', { value: formatFixed(entry.valueKg, 1) })}</td></tr>
                ))}
              </tbody>
            </ResponsiveTable>
          </details>
        </>
      )}
    </div></div>
  );
}
