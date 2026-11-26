import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { EmptyState, PageState, ResponsiveTable } from '../components/ui';
import { formatNumber, localDay } from '../format';
import { useApi, usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { useAthlete } from '../session';

interface PersonalRecord {
  muscleGroupName: string;
  muscleSubGroupName: string;
  exerciseName: string;
  maxWeightKg: number;
  achievedAt: string;
}

interface Statistics {
  workoutsCompleted: number;
  totalWorkoutMinutes: number;
  avgWorkoutDurationMinutes: number;
  longestStreak: number;
  avgWeeklyWorkouts: number;
  totalSetsCount: number;
  avgSetsPerWorkout: number;
  avgRestSeconds: number;
  totalVolume: number;
  monthlyTotalTonnage: number;
  weeklyProgress: number;
  last7DaysWorkouts: number;
  last7DaysMinutes: number;
  uniqueExercisesCount: number;
  exercisesThisMonth: number;
  favoriteExerciseName: string | null;
  favoriteExerciseCount: number;
  bestProgressPercent: number;
  bestProgressExercise: string | null;
  totalAwardsCount: number;
  recentRecords: PersonalRecord[];
}

interface Award {
  code: string;
  name: string;
  conditionDescription: string;
  isUnlocked: boolean;
  isNumeric: boolean;
  currentValue: number;
  targetValue: number;
  progressPercent: number;
}

function Stat({ label, value, unit, note }: { label: string; value: ReactNode; unit?: string; note?: ReactNode }) {
  return (
    <div className="card stat-card h-100"><div className="card-body">
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}{unit && <small> {unit}</small>}</div>
      {note && <p className="stat-note">{note}</p>}
    </div></div>
  );
}

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
  if (!data) return <PageState error={error} loading={loading} />;
  const volume = t('statistics.volumeUnit');
  const minutes = t('unit.minShort');

  return (
    <>
      <h2>{t('statistics.heading')}</h2>
      <p className="small text-muted">{t('statistics.note')}</p>
      <StatRow>
        <Stat label={t('statistics.workouts')} value={data.workoutsCompleted} />
        <Stat label={t('statistics.time')} value={data.totalWorkoutMinutes} unit={minutes} />
        <Stat label={t('statistics.avgDuration')} value={formatNumber(data.avgWorkoutDurationMinutes)} unit={minutes} />
      </StatRow>
      <StatRow>
        <Stat label={t('statistics.streak')} value={t('count.days', { count: data.longestStreak })} />
        <Stat label={t('statistics.monthVolume')} value={formatNumber(data.monthlyTotalTonnage)} unit={volume} />
        <Stat label={t('statistics.avgRest')} value={data.avgRestSeconds} unit={t('unit.secShort')} />
      </StatRow>
      <StatRow>
        <Stat label={t('statistics.bestChange')} value={`${formatNumber(data.bestProgressPercent)}%`}
          note={data.bestProgressExercise ?? t('statistics.bestChangeNone')} />
        <Stat label={t('statistics.awards')} value={data.totalAwardsCount} note={<Link to="/awards">{t('profile.allAwards')}</Link>} />
      </StatRow>

      <h4 className="mt-5 mb-3">{t('statistics.volumeHeading')}</h4>
      <StatRow>
        <Stat label={t('statistics.totalVolume')} value={formatNumber(data.totalVolume)} unit={volume} note={t('statistics.totalVolumeNote')} />
        <Stat label={t('statistics.sets')} value={data.totalSetsCount} note={t('statistics.setsNote')} />
        <Stat label={t('statistics.setsPerWorkout')} value={formatNumber(data.avgSetsPerWorkout)} note={t('statistics.setsPerWorkoutNote')} />
      </StatRow>
      <StatRow>
        <Stat label={t('statistics.unique')} value={data.uniqueExercisesCount} note={t('statistics.uniqueNote')} />
        <Stat label={t('statistics.month')} value={data.exercisesThisMonth} note={t('statistics.monthNote')} />
        <Stat label={t('statistics.frequency')} value={formatNumber(data.avgWeeklyWorkouts)} note={t('statistics.frequencyNote')} />
      </StatRow>

      <StatRow>
        <div>
          <h4 className="mb-3">{t('statistics.weekHeading')}</h4>
          <Stat label={t('statistics.weekVolume')} value={formatNumber(data.weeklyProgress)} unit={volume}
            note={`${t('count.workouts', { count: data.last7DaysWorkouts })} · ${t('unit.min', { value: data.last7DaysMinutes })}`} />
        </div>
        <div>
          <h4 className="mb-3">{t('statistics.favoriteHeading')}</h4>
          <Stat label={t('statistics.favorite')} value={data.favoriteExerciseName ?? '—'}
            note={data.favoriteExerciseName ? t('statistics.favoriteCount', { count: data.favoriteExerciseCount }) : t('statistics.favoriteNone')} />
        </div>
      </StatRow>

      <div className="section-heading">
        <h4>{t('statistics.recentRecords')}</h4>
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
      <h2>{t('records.title')}</h2>
      <p className="small text-muted">{t('records.note')}</p>
      {!data && <PageState error={error} loading={loading} />}
      {data?.length === 0 && <p className="text-muted">{t('statistics.noRecords')}</p>}
      {data && data.length > 0 && <RecordsTable records={data} grouped />}
      <Link to="/statistics" className="btn btn-outline-secondary mt-3">{t('records.back')}</Link>
    </>
  );
}

export function AwardsPage() {
  const { t } = useI18n();
  usePageTitle(t('awards.title'));
  const { data, error, loading } = useApi<Award[]>('/api/awards');
  if (!data) return <PageState error={error} loading={loading} />;

  return (
    <>
      <div className="section-heading mt-0">
        <h2 className="mb-0">{t('awards.title')}</h2>
        <span className="text-muted small">{t('awards.summary', { unlocked: data.filter((a) => a.isUnlocked).length, total: data.length })}</span>
      </div>
      <p className="text-muted">{t('awards.intro')}</p>
      <div className="row g-3">
        {data.map((award) => {
          const percent = Math.round(award.progressPercent);
          return (
            <div key={award.code} className="col-md-6 col-lg-4">
              <div className={`card h-100${award.isUnlocked ? ' border border-light' : ''}`}><div className="card-body d-flex flex-column">
                <div className="d-flex justify-content-between align-items-start mb-2 gap-2">
                  <h5 className="mb-0">{award.name}</h5>
                  {award.isUnlocked && <span className="badge bg-light text-dark" title={t('awards.unlocked')}>✓<span className="visually-hidden"> {t('awards.unlocked')}</span></span>}
                </div>
                <div className="small text-muted mb-3">{award.conditionDescription}</div>
                <div className="progress mb-2" role="progressbar" aria-label={award.name} aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
                  <div className={`progress-bar ${award.isUnlocked ? 'bg-light text-dark' : 'bg-secondary'}`} style={{ width: `${percent}%` }}></div>
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
