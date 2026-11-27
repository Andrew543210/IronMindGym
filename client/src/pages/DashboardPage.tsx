import { Link, useParams } from 'react-router';
import type { WorkoutListItem } from '../api/types/common';
import type { Overview, ResearchNote } from '../api/types/dashboard';
import { StatCard } from '../components/StatCard';
import { EmptyState, Glyph, PageState } from '../components/ui';
import { formatDay, formatFixed, formatNumber, localFormat } from '../format';
import { useApi, usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { useAthlete } from '../session';

const whole = (value: number | undefined) => (value === undefined ? '—' : formatNumber(value, 0));

export function DashboardPage() {
  const { t } = useI18n();
  usePageTitle(t('nav.overview'));
  const athlete = useAthlete();
  const { data, error, loading } = useApi<Overview>('/api/dashboard');
  if (!data) return <PageState error={error} loading={loading} />;

  const weeklyPercent = data.plannedWorkoutsPerWeek === 0
    ? 0
    : Math.min(100, Math.floor((data.completedThisWeek * 100) / data.plannedWorkoutsPerWeek));
  const zone = athlete.timeZone;
  const coefficient = formatFixed(data.strengthCoefficient, 2);

  return (
    <>
      <div className="dashboard-greeting">
        <div>
          <p className="eyebrow">{t('dashboard.eyebrow')}</p>
          <h1 className="h2">{t('dashboard.greeting', { name: data.username })}</h1>
        </div>
        <span className="date-label">{localFormat(new Date(), zone, { weekday: 'long', month: 'long', day: 'numeric' })} · {zone}</span>
      </div>
      {data.needsSetup && (
        <div className="alert alert-info mt-4">{t('dashboard.needsSetup')} <Link to="/setup">{t('dashboard.setupLink')} <Glyph>→</Glyph></Link></div>
      )}

      <section className="dashboard-hero" aria-label={t('dashboard.heroLabel')}>
        <img src="/images/photo.jpg" alt="" />
        <div className="hero-copy">
          <p className="eyebrow">{t('dashboard.heroEyebrow')}</p>
          <p className="hero-title">{t('dashboard.hero1')}<br />{t('dashboard.hero2')}</p>
          <p>{t('dashboard.heroText')}</p>
          <Link className="btn btn-primary mt-2" to="/workouts/new">{t('dashboard.start')} <span aria-hidden="true">↗</span></Link>
        </div>
      </section>

      <div className="row g-3">
        <div className="col-6 col-xl-3">
          <StatCard label={t('dashboard.thisWeek')} value={data.completedThisWeek} unit={t('dashboard.weekPlan', { count: data.plannedWorkoutsPerWeek })}>
            <div className="weekly-meter" role="img" aria-label={t('dashboard.weeklyMeter', { percent: weeklyPercent })}><span style={{ width: `${weeklyPercent}%` }}></span></div>
          </StatCard>
        </div>
        <div className="col-6 col-xl-3">
          <StatCard label={t('dashboard.strength')} value={coefficient} unit={t('dashboard.timesBodyweight')} note={t('dashboard.strengthNote')} />
        </div>
        <div className="col-6 col-xl-3">
          <StatCard label={t('dashboard.bodyweight')} value={data.weightKg === null ? '—' : formatFixed(data.weightKg, 1)} unit={t('unit.kgShort')}
            note={<Link to="/profile">{t('dashboard.weighIn')} <Glyph>↗</Glyph></Link>} />
        </div>
        <div className="col-6 col-xl-3">
          <StatCard label={t('dashboard.sessions')} value={data.totalWorkouts} note={t('dashboard.sessionsNote')} />
        </div>
      </div>

      <div className="section-heading">
        <h2 className="h4">{t('dashboard.journey')}</h2>
        <Link className="small text-muted" to="/progress">{t('dashboard.viewProgress')} <Glyph>↗</Glyph></Link>
      </div>
      <section className="card"><div className="card-body">
        <div className="d-flex flex-wrap justify-content-between gap-2">
          <div>
            <p className="eyebrow">{t('dashboard.rank')}</p>
            <h3 className="mb-1">{data.avatarName}</h3>
            <p className="text-muted small mb-0">{t('dashboard.rankNote')}</p>
          </div>
          <span className="stat-value">{coefficient}<small> / {formatFixed(5, 1)}</small></span>
        </div>
        <div className="rank-meter" role="meter" aria-label={t('dashboard.rankScale')} aria-valuemin={0} aria-valuemax={100}
          aria-valuenow={Math.round(data.currentAvatarPositionPercent)}>
          <span style={{ width: `${data.currentAvatarPositionPercent.toFixed(1)}%` }}></span>
        </div>
        <details className="mt-3">
          <summary className="small text-muted">{t('dashboard.allRanks')}</summary>
          <div className="rank-grid">
            {data.avatarScalePoints.map((rank) => (
              <div key={rank.name} className={`rank-item${rank.isCurrent ? ' current' : ''}`} aria-current={rank.isCurrent || undefined}>
                {t('dashboard.rankThreshold', { value: formatFixed(rank.minStrengthCoefficient, 1) })}<strong>{rank.name}</strong>
              </div>
            ))}
          </div>
        </details>
      </div></section>

      <div className="row g-4 mt-1">
        <div className="col-lg-7">
          <div className="section-heading">
            <h2 className="h4">{t('dashboard.recent')}</h2>
            <Link className="small text-muted" to="/workouts">{t('dashboard.history')} <Glyph>↗</Glyph></Link>
          </div>
          <div className="card"><div className="card-body">
            {data.recentWorkouts.length === 0 && (
              <EmptyState title={t('dashboard.emptyTitle')}>{t('dashboard.emptyText')}</EmptyState>
            )}
            {data.recentWorkouts.map((session) => <SessionRow key={session.id} session={session} timeZone={zone} />)}
          </div></div>
        </div>
        <div className="col-lg-5">
          <div className="section-heading">
            <h2 className="h4">{t('dashboard.nutrition')}</h2>
            <Link className="small text-muted" to="/nutrition">{t('dashboard.logFood')} <Glyph>↗</Glyph></Link>
          </div>
          <div className="card"><div className="card-body">
            <p className="stat-label">{t('dashboard.energy')}</p>
            <div className="stat-value">{whole(data.todayNutrition?.caloriesKcal)} <small>{t('unit.kcalShort')}</small></div>
            <div className="macro-grid mt-4">
              <div><span>{whole(data.todayNutrition?.proteinG)}<small> {t('unit.gShort')}</small></span><p>{t('macro.protein')}</p></div>
              <div><span>{whole(data.todayNutrition?.carbsG)}<small> {t('unit.gShort')}</small></span><p>{t('macro.carbs')}</p></div>
              <div><span>{whole(data.todayNutrition?.fatG)}<small> {t('unit.gShort')}</small></span><p>{t('macro.fat')}</p></div>
            </div>
          </div></div>
        </div>
      </div>

      <div className="section-heading">
        <h2 className="h4">{t('dashboard.milestones')}</h2>
        <Link className="small text-muted" to="/goals">{t('dashboard.manageGoals')} <Glyph>↗</Glyph></Link>
      </div>
      <div className="card"><div className="card-body">
        {data.goals.length === 0 && (
          <p className="text-muted mb-0">{t('dashboard.noGoals')} <Link to="/goals">{t('dashboard.firstGoal')} <Glyph>→</Glyph></Link></p>
        )}
        {data.goals.map((goal) => (
          <div className="goal-row" key={goal.id}>
            <strong>{goal.title}</strong>
            <span className="small text-muted">{formatDay(goal.deadline, { month: 'short', day: 'numeric' })}</span>
          </div>
        ))}
      </div></div>

      <div className="section-heading">
        <h2 className="h4">{t('dashboard.research')}</h2>
        <span className="small text-muted">{t('dashboard.researchNote')}</span>
      </div>
      <div className="feed-grid">
        {data.feedItems.map((item) => (
          <article className="feed-card" key={item.id}>
            <span className="eyebrow">{t('dashboard.trainingNotes')}</span>
            <h3 className="h5">{item.title}</h3>
            <p>{item.thesisText}</p>
            <div className="source-label mb-3">{item.source}</div>
            <div className="d-flex flex-wrap gap-2">
              {item.sourceUrl && <a className="btn btn-outline-secondary btn-sm" href={item.sourceUrl} target="_blank" rel="noopener noreferrer">{t('dashboard.readSource')} <Glyph>↗</Glyph></a>}
              <Link className="btn btn-outline-secondary btn-sm" to={`/feed/${item.id}`}>{t('dashboard.viewNote')}</Link>
            </div>
          </article>
        ))}
      </div>
    </>
  );
}

function SessionRow({ session, timeZone }: { session: WorkoutListItem; timeZone: string }) {
  const { t } = useI18n();
  return (
    <Link className="session-row" to={`/workouts/${session.id}`}>
      <span className="session-date">
        {localFormat(session.sessionDate, timeZone, { day: '2-digit' })}
        <small>{localFormat(session.sessionDate, timeZone, { month: 'short' })}</small>
      </span>
      <span>
        <strong>{session.name}</strong>
        <small>{t('count.minutes', { count: session.durationMinutes })} · {localFormat(session.sessionDate, timeZone, { weekday: 'short' })}</small>
      </span>
      <span className="ms-auto text-muted" aria-hidden="true">↗</span>
    </Link>
  );
}

export function FeedNotePage() {
  const { t } = useI18n();
  usePageTitle(t('feed.title'));
  const { id } = useParams();
  const { data, error, loading } = useApi<ResearchNote>(`/api/feed/${Number(id)}`);
  if (!data || loading) return <PageState error={error} loading={loading} />;
  return (
    <>
      <h1 className="h2">{t('feed.title')}</h1>
      <div className="card"><div className="card-body">
        <div className="small text-muted mb-2">{t('feed.year', { year: data.publishedAt.slice(0, 4) })}</div>
        <h2 className="h4 mb-3">{data.title}</h2>
        <p>{data.thesisText}</p>
        <p className="mb-3"><strong>{t('feed.source')}</strong> {data.source}</p>
        {data.sourceUrl && <a href={data.sourceUrl} target="_blank" rel="noopener noreferrer" className="btn btn-light">{t('feed.read')}</a>}
      </div></div>
      <div className="mt-3"><Link to="/" className="btn btn-outline-light">{t('feed.back')}</Link></div>
    </>
  );
}
