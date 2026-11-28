import type { ReactNode } from 'react';
import { Link } from 'react-router';
import type { AwardProgress, PersonalRecord, Statistics } from '../api/types/statistics';
import { StatCard } from '../components/StatCard';
import { EmptyState, Glyph, PageState, ResponsiveTable } from '../components/ui';
import { formatNumber, localDay } from '../format';
import { useApi, usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { useAthlete } from '../session';

function StatRow({ children }: { children: ReactNode[] }) {
  const width = children.length === 2 ? 'col-md-6' : 'col-md-4';
  return <div className="row g-3 mb-4">{children.map((child, index) => <div key={index} className={width}>{child}</div>)}</div>;
}

function RecordsTable({ records, grouped }: { records: PersonalRecord[]; grouped: boolean }) {
  const { t } = useI18n();
  const athlete = useAthlete();
  return (
    <ResponsiveTable className={grouped ? 'table table-striped table-sm' : 'table'}>
      <thead>
        <tr>
          {grouped && <><th>{t('records.group')}</th><th>{t('records.focus')}</th></>}
          <th>{t('common.exercise')}</th><th>{t('statistics.heaviest')}</th><th>{t('common.date')}</th>
        </tr>
      </thead>
      <tbody>
        {records.map((record) => (
          <tr key={`${record.muscleGroupName}:${record.exerciseName}`}>
            {grouped && <><td>{record.muscleGroupName}</td><td>{record.muscleSubGroupName}</td></>}
            <td>{record.exerciseName}</td>
            <td className="metric">{t('unit.kg', { value: formatNumber(record.maxWeightKg) })}</td>
            <td className="metric">{localDay(record.achievedAt, athlete.timeZone)}</td>
          </tr>
        ))}
      </tbody>
    </ResponsiveTable>
  );
}

export function StatisticsPage() {
  const { t } = useI18n();
  usePageTitle(t('statistics.title'));
  const { data, error, loading } = useApi<Statistics>('/api/statistics');
  if (!data) return <PageState error={error} loading={loading} title={t('statistics.heading')} />;
  const volume = t('statistics.volumeUnit');
  const minutes = t('unit.minShort');

  return (
    <>
      <h1 className="h2">{t('statistics.heading')}</h1>
      <p className="small text-muted">{t('statistics.note')}</p>
      <StatRow>
        <StatCard label={t('statistics.workouts')} value={data.workoutsCompleted} />
        <StatCard label={t('statistics.time')} value={data.totalWorkoutMinutes} unit={minutes} />
        <StatCard label={t('statistics.avgDuration')} value={formatNumber(data.avgWorkoutDurationMinutes)} unit={minutes} />
      </StatRow>
      <StatRow>
        <StatCard label={t('statistics.streak')} value={t('count.days', { count: data.longestStreak })} />
        <StatCard label={t('statistics.monthVolume')} value={formatNumber(data.monthlyTotalTonnage)} unit={volume} />
        <StatCard label={t('statistics.avgRest')} value={data.avgRestSeconds} unit={t('unit.secShort')} />
      </StatRow>
      <StatRow>
        <StatCard label={t('statistics.bestChange')} value={`${formatNumber(data.bestProgressPercent)}%`}
          note={data.bestProgressExercise ?? t('statistics.bestChangeNone')} />
        <StatCard label={t('statistics.awards')} value={data.totalAwardsCount} note={<Link to="/awards">{t('profile.allAwards')} <Glyph>↗</Glyph></Link>} />
      </StatRow>

      <h2 className="h4 mt-5 mb-3">{t('statistics.volumeHeading')}</h2>
      <StatRow>
        <StatCard label={t('statistics.totalVolume')} value={formatNumber(data.totalVolume)} unit={volume} note={t('statistics.totalVolumeNote')} />
        <StatCard label={t('statistics.sets')} value={data.totalSetsCount} note={t('statistics.setsNote')} />
        <StatCard label={t('statistics.setsPerWorkout')} value={formatNumber(data.avgSetsPerWorkout)} note={t('statistics.setsPerWorkoutNote')} />
      </StatRow>
      <StatRow>
        <StatCard label={t('statistics.unique')} value={data.uniqueExercisesCount} note={t('statistics.uniqueNote')} />
        <StatCard label={t('statistics.month')} value={data.exercisesThisMonth} note={t('statistics.monthNote')} />
        <StatCard label={t('statistics.frequency')} value={formatNumber(data.avgWeeklyWorkouts)} note={t('statistics.frequencyNote')} />
      </StatRow>

      <StatRow>
        <div>
          <h2 className="h4 mb-3">{t('statistics.weekHeading')}</h2>
          <StatCard label={t('statistics.weekVolume')} value={formatNumber(data.weeklyProgress)} unit={volume}
            note={`${t('count.workouts', { count: data.last7DaysWorkouts })} · ${t('unit.min', { value: data.last7DaysMinutes })}`} />
        </div>
        <div>
          <h2 className="h4 mb-3">{t('statistics.favoriteHeading')}</h2>
          <StatCard label={t('statistics.favorite')} value={data.favoriteExerciseName ?? '—'}
            note={data.favoriteExerciseName ? t('statistics.favoriteCount', { count: data.favoriteExerciseCount }) : t('statistics.favoriteNone')} />
        </div>
      </StatRow>

      <div className="section-heading">
        <h2 className="h4">{t('statistics.recentRecords')}</h2>
        <Link className="small text-muted" to="/statistics/records">{t('statistics.allRecords')}</Link>
      </div>
      <p className="small text-muted">{t('statistics.recordsNote')}</p>
      {data.recentRecords.length === 0
        ? <EmptyState title={t('statistics.noRecords')}><Link to="/workouts/new">{t('dashboard.start')}</Link></EmptyState>
        : <RecordsTable records={data.recentRecords} grouped={false} />}
    </>
  );
}

export function RecordsPage() {
  const { t } = useI18n();
  usePageTitle(t('records.title'));
  const { data, error, loading } = useApi<PersonalRecord[]>('/api/statistics/records');
  return (
    <>
      <h1 className="h2">{t('records.title')}</h1>
      <p className="small text-muted">{t('records.note')}</p>
      {!data && <PageState error={error} loading={loading} />}
      {data?.length === 0 && <p className="text-muted">{t('statistics.noRecords')}</p>}
      {data && data.length > 0 && <RecordsTable records={data} grouped />}
      <Link to="/statistics" className="btn btn-outline-secondary mt-3"><Glyph>←</Glyph> {t('records.back')}</Link>
    </>
  );
}

export function AwardsPage() {
  const { t } = useI18n();
  usePageTitle(t('awards.title'));
  const { data, error, loading } = useApi<AwardProgress[]>('/api/awards');
  if (!data) return <PageState error={error} loading={loading} title={t('awards.title')} />;

  return (
    <>
      <div className="section-heading mt-0">
        <h1 className="h2 mb-0">{t('awards.title')}</h1>
        <span className="text-muted small">{t('awards.summary', { unlocked: data.filter((a) => a.isUnlocked).length, total: data.length })}</span>
      </div>
      <p className="text-muted">{t('awards.intro')}</p>
      <div className="row g-3">
        {data.map((award) => {
          const percent = Math.round(award.progressPercent);
          return (
            <div key={award.code} className="col-md-6 col-lg-4">
              <div className="card h-100"><div className="card-body d-flex flex-column">
                <div className="d-flex justify-content-between align-items-start mb-2 gap-2">
                  <h2 className="h5 mb-0">{award.name}</h2>
                  {award.isUnlocked && <span className="badge text-bg-light" title={t('awards.unlocked')}>✓<span className="visually-hidden"> {t('awards.unlocked')}</span></span>}
                </div>
                <div className="small text-muted mb-3">{award.conditionDescription}</div>
                <div className="progress mb-2" role="progressbar" aria-label={award.name} aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
                  <div className={`progress-bar ${award.isUnlocked ? 'bg-light' : 'bg-secondary'}`} style={{ width: `${percent}%` }}></div>
                </div>
                <div className={`small mt-auto${award.isNumeric ? '' : ' text-muted'}`}>
                  {award.isNumeric
                    ? t('awards.progress', { current: formatNumber(award.currentValue), target: formatNumber(award.targetValue), percent })
                    : t('awards.nonNumeric')}
                </div>
              </div></div>
            </div>
          );
        })}
      </div>
    </>
  );
}
