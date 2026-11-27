import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { api } from '../api/client';
import type { Intake } from '../api/types/common';
import type { NutritionAdvice, NutritionDay, Target } from '../api/types/nutrition';
import { ErrorAlert, Glyph, PageState, ResponsiveTable } from '../components/ui';
import { formatDay, formatFixed, formatNumber } from '../format';
import { textFields } from '../forms';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n, type MessageKey, type Translate } from '../i18n/i18n';
import { useAthlete } from '../session';

const macros = [
  { field: 'caloriesKcal', target: 'calories', label: 'field.caloriesKcal', name: 'nutrition.energy', title: 'nutrition.caloriesTarget', unit: 'unit.kcalShort', max: 10000, estimate: 'nutrition.caloriesEstimate' },
  { field: 'proteinG', target: 'protein', label: 'field.proteinG', name: 'macro.protein', title: 'nutrition.proteinTarget', unit: 'unit.gShort', max: 500, estimate: 'nutrition.proteinEstimate' },
  { field: 'fatG', target: 'fat', label: 'field.fatG', name: 'macro.fat', title: 'nutrition.fatTarget', unit: 'unit.gShort', max: 300, estimate: 'nutrition.fatEstimate' },
  { field: 'carbsG', target: 'carbs', label: 'field.carbsG', name: 'macro.carbs', title: 'nutrition.carbsTarget', unit: 'unit.gShort', max: 1000, estimate: 'nutrition.carbsRemainder' },
] as const satisfies readonly { field: keyof Intake; target: keyof NutritionAdvice; label: MessageKey; name: MessageKey; title: MessageKey; unit: MessageKey; max: number; estimate: MessageKey }[];

function targetText(t: Translate, target: Target, unit: MessageKey) {
  return target.source === 'NotSet' ? '—' : `${formatNumber(target.value, 0)} ${t(unit)}`;
}

function sourceHint(t: Translate, target: Target, estimate: MessageKey, carbs: boolean): string {
  if (target.source === 'Personal') return t('nutrition.personal');
  if (target.source === 'NotSet') return t(carbs ? 'nutrition.carbsNotSet' : 'nutrition.notSet');
  return t(estimate);
}

function tips(t: Translate, day: NutritionDay): string[] {
  const { advice } = day;
  const result: string[] = [];
  if (advice.targetsSet === 0) result.push(t('nutrition.setTargets'));
  if (advice.targetsExceedEnergy) result.push(t('nutrition.exceeds'));
  for (const macro of macros) {
    const target = advice[macro.target];
    if (target.value > 0)
      result.push(t('nutrition.line', { name: t(macro.name), logged: formatNumber(day[macro.field], 0), target: formatNumber(target.value, 0) }));
  }
  if (advice.macrosDisagreeWithEnergy) result.push(t('nutrition.macroEnergy', { kcal: formatNumber(advice.macroEnergyKcal, 0) }));
  if (advice.weekDaysLogged > 0)
    result.push(t('nutrition.weekAverage', { count: advice.weekDaysLogged, kcal: formatNumber(advice.weekAverageKcal, 0) }));
  return result;
}

export function NutritionPage() {
  const { t } = useI18n();
  usePageTitle(t('nutrition.title'));
  const athlete = useAthlete();
  const [params, setParams] = useSearchParams();
  const date = params.get('date') ?? athlete.today;
  const { data, error, loading, reload } = useApi<NutritionDay>(`/api/nutrition?date=${encodeURIComponent(date)}`);
  const [change, setChange] = useState<DayChange>();

  function choose(day: string) {
    setChange(undefined);
    setParams(day === athlete.today ? {} : { date: day }, { replace: true });
  }

  return (
    <>
      <p className="eyebrow">{t('nutrition.eyebrow')}</p>
      <h1 className="h2">{t('nutrition.heading')}</h1>
      <p className="text-muted">{t('nutrition.intro')}</p>
      {change && (
        <div className="alert alert-success" role="status">
          {change.deleted ? t('nutrition.deleted') : t('nutrition.saved', { date: formatDay(change.day) })}
        </div>
      )}
      {!data && <PageState error={error} loading={loading} />}
      {data && (
        <>
          <DayForm key={data.date} day={data} chosenDate={date} onChoose={choose}
            onChanged={(next) => { setChange(next); reload(); }} />
          <Targets advice={data.advice} />
          {data.isLogged && (
            <div className="alert alert-info mt-4">
              <div><strong>{t('nutrition.comparison')}:</strong> {t('nutrition.summary')}</div>
              <div className="mt-1">
                <span>
                  {data.advice.targetsSet === 0
                    ? t('nutrition.noTargets')
                    : t('nutrition.matched', { matched: data.advice.targetsMatched, total: data.advice.targetsSet })}
                </span>
                {' · '}<span className="metric">{t('nutrition.score', { score: data.advice.score })}</span>
              </div>
              <hr />
              <ul className="mb-0">{tips(t, data).map((tip) => <li key={tip}>{tip}</li>)}</ul>
            </div>
          )}
          <RecentEntries day={data} onChoose={choose} />
        </>
      )}
    </>
  );
}

/** What the last change did; the message is worded when shown, so it follows a change of the interface language. */
interface DayChange {
  day: string;
  deleted: boolean;
}

function DayForm({ day, chosenDate, onChoose, onChanged }: {
  day: NutritionDay;
  chosenDate: string;
  onChoose: (date: string) => void;
  onChanged: (change: DayChange) => void;
}) {
  const { t } = useI18n();
  const athlete = useAthlete();
  const dayKey = day.date;
  const [form, setForm] = useState(() => Object.fromEntries(
    macros.map((m) => [m.field, day.isLogged ? String(day[m.field]) : '']),
  ) as Record<keyof Intake, string>);
  const { busy, error, run } = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    void run(async () => {
      await api.put(`/api/nutrition/days/${dayKey}`, Object.fromEntries(macros.map((m) => [m.field, Number(form[m.field])])));
      onChanged({ day: dayKey, deleted: false });
    });
  }

  function remove() {
    if (!window.confirm(t('nutrition.confirmDelete', { date: formatDay(dayKey) }))) return;
    void run(async () => {
      await api.delete(`/api/nutrition/days/${dayKey}`);
      onChanged({ day: dayKey, deleted: true });
    });
  }

  const field = textFields(form, setForm);
  return (
    <form className="row g-3" onSubmit={submit}>
      <ErrorAlert error={error} />
      <div className="col-12 col-md-4">
        <label htmlFor="date" className="form-label">{t('nutrition.date')}</label>
        <input id="date" type="date" className="form-control" required max={athlete.today} value={chosenDate}
          onChange={(e) => e.target.value && onChoose(e.target.value)} />
        {!day.isLogged && <p className="form-text mb-0">{t('nutrition.notLogged')}</p>}
      </div>
      <div className="w-100"></div>
      {macros.map((macro) => (
        <div className="col-6 col-md-3" key={macro.field}>
          <label htmlFor={macro.field} className="form-label">{t(macro.label)}</label>
          <input {...field(macro.field)} type="number" inputMode="decimal" step="any" min={0} max={macro.max} required className="form-control" />
        </div>
      ))}
      <div className="col-12 d-flex flex-wrap gap-2">
        <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t('common.saving') : t('common.save')}</button>
        {day.isLogged && <button type="button" className="btn btn-outline-danger" disabled={busy} onClick={remove}>{t('nutrition.delete')}</button>}
      </div>
    </form>
  );
}

function Targets({ advice }: { advice: NutritionAdvice }) {
  const { t } = useI18n();
  const estimating = advice.energyEquation !== null;
  const tiles: { label: string; value: string; hint: string }[] = [
    {
      label: t('nutrition.bmr'),
      value: estimating ? `${formatNumber(advice.basalMetabolicRate, 0)} ${t('unit.kcalShort')}` : t('nutrition.off'),
      hint: estimating
        ? t('nutrition.bmrFormula', {
          equation: t(advice.energyEquation === 'Male' ? 'nutrition.male' : 'nutrition.female'),
          constant: advice.energyEquation === 'Male' ? '+ 5' : '− 161',
        })
        : t('nutrition.estimatesOff'),
    },
    {
      label: t('nutrition.activity'),
      value: estimating ? formatFixed(advice.activityMultiplier, 2) : t('nutrition.off'),
      hint: estimating ? t('nutrition.activityHint') : t('nutrition.estimatesOff'),
    },
    ...macros.map((macro) => ({
      label: t(macro.title),
      value: targetText(t, advice[macro.target], macro.unit),
      hint: sourceHint(t, advice[macro.target], macro.estimate, macro.target === 'carbs'),
    })),
  ];

  return (
    <div className="card mt-4"><div className="card-body">
      <div className="section-heading mt-0">
        <h2 className="h5">{t('nutrition.targets')}</h2>
        <Link className="small text-muted" to="/profile/settings">{t('nutrition.editTargets')} <Glyph>↗</Glyph></Link>
      </div>
      <p className="small mb-3">{t('nutrition.priority')}</p>
      <div className="row g-2">
        {tiles.map((tile) => (
          <div className="col-md-4" key={tile.label}>
            <div className="formula-target p-2 h-100">
              <div>{tile.label}: <span className="metric">{tile.value}</span></div>
              <div className="formula-hint">{tile.hint}</div>
            </div>
          </div>
        ))}
      </div>
    </div></div>
  );
}

function RecentEntries({ day, onChoose }: { day: NutritionDay; onChoose: (date: string) => void }) {
  const { t } = useI18n();
  const selected = day.date;
  return (
    <div className="card mt-4"><div className="card-body">
      <h2 className="h5">{t('nutrition.recent')}</h2>
      {day.recentLogs.length === 0 ? <p className="mb-0">{t('nutrition.none')}</p> : (
        <ResponsiveTable className="table table-sm mt-3 mb-0">
          <thead>
            <tr>
              <th>{t('common.date')}</th><th>{t('macro.kcal')}</th><th>{t('macro.p')}</th><th>{t('macro.f')}</th><th>{t('macro.c')}</th>
              <th><span className="visually-hidden">{t('common.edit')}</span></th>
            </tr>
          </thead>
          <tbody>
            {day.recentLogs.map((log) => {
              const logDay = log.date;
              return (
                <tr key={logDay}>
                  <td className="metric">{logDay}</td>
                  <td className="metric">{formatNumber(log.caloriesKcal, 0)}</td>
                  <td className="metric">{formatNumber(log.proteinG)}</td>
                  <td className="metric">{formatNumber(log.fatG)}</td>
                  <td className="metric">{formatNumber(log.carbsG)}</td>
                  <td className="text-end">
                    {logDay === selected
                      ? <span className="badge text-bg-light">{t('nutrition.selected')}</span>
                      : <button type="button" className="btn btn-outline-light btn-sm" onClick={() => onChoose(logDay)}>{t('common.edit')}</button>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </ResponsiveTable>
      )}
    </div></div>
  );
}
