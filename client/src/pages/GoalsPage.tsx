import { useState, type FormEvent } from 'react';
import { api } from '../api';
import { EmptyState, ErrorAlert, PageState } from '../components/ui';
import { formatDay } from '../format';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { useAthlete } from '../session';

export interface GoalItem {
  id: number;
  title: string;
  deadline: string;
  isCompleted: boolean;
}

function inOneMonth(today: string): string {
  const date = new Date(`${today}T12:00:00Z`);
  date.setUTCMonth(date.getUTCMonth() + 1);
  return date.toISOString().slice(0, 10);
}

export function GoalsPage() {
  const { t } = useI18n();
  usePageTitle(t('goals.title'));
  const athlete = useAthlete();
  const { data: goals, error: loadError, loading, reload } = useApi<GoalItem[]>('/api/goals');
  const [form, setForm] = useState(() => ({ title: '', deadline: inOneMonth(athlete.today) }));
  const create = useAction();
  const change = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    const sent = form;
    void create.run(async () => {
      await api.post('/api/goals', sent);
      setForm((current) => (current.title === sent.title ? { ...current, title: '' } : current));
      reload();
    });
  }

  function setCompleted(goal: GoalItem) {
    void change.run(async () => {
      await api.put(`/api/goals/${goal.id}/completed`, { completed: !goal.isCompleted });
      reload();
    });
  }

  function remove(goal: GoalItem) {
    if (!window.confirm(t('goals.confirmDelete', { title: goal.title }))) return;
    void change.run(async () => {
      await api.delete(`/api/goals/${goal.id}`);
      reload();
    });
  }

  return (
    <>
      <p className="eyebrow">{t('goals.eyebrow')}</p>
      <h2>{t('goals.heading')}</h2>
      <div className="row g-4">
        <div className="col-lg-4">
          <div className="card"><div className="card-body">
            <h4>{t('goals.next')}</h4>
            <p className="text-muted small">{t('goals.hint')}</p>
            <form className="row g-3" onSubmit={submit}>
              <ErrorAlert error={create.error} />
              <div className="col-12">
                <label htmlFor="title" className="form-label">{t('goals.what')}</label>
                <input id="title" className="form-control" placeholder={t('goals.placeholder')} required minLength={3} maxLength={80}
                  value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
              </div>
              <div className="col-12">
                <label htmlFor="deadline" className="form-label">{t('goals.date')}</label>
                <input id="deadline" type="date" className="form-control" required min={athlete.today}
                  value={form.deadline} onChange={(e) => setForm({ ...form, deadline: e.target.value })} />
              </div>
              <div className="col-12">
                <button className="btn btn-primary w-100" type="submit" disabled={create.busy}>{t('goals.add')}</button>
              </div>
            </form>
          </div></div>
        </div>
        <div className="col-lg-8">
          <div className="card"><div className="card-body">
            <div className="section-heading mt-0">
              <h4>{t('goals.list')}</h4>
              {goals && (
                <span className="text-muted small">{t('goals.done', { done: goals.filter((g) => g.isCompleted).length, total: goals.length })}</span>
              )}
            </div>
            <ErrorAlert error={change.error} />
            {!goals && <PageState error={loadError} loading={loading} />}
            {goals?.length === 0 && (
              <EmptyState title={t('goals.emptyTitle')}>{t('goals.emptyText')}</EmptyState>
            )}
            {goals?.map((goal) => (
              <div key={goal.id} className={`goal-row${goal.isCompleted ? ' completed' : ''}`}>
                <div>
                  <strong>{goal.title}</strong>
                  <p className="small text-muted mb-0">
                    {goal.isCompleted ? t('goals.completed') : goal.deadline < athlete.today ? t('goals.overdue') : t('goals.inProgress')}
                    {' · '}{formatDay(goal.deadline)}
                  </p>
                </div>
                <div className="d-flex gap-2">
                  <button type="button" className="btn btn-outline-secondary btn-sm" disabled={change.busy} onClick={() => setCompleted(goal)}>
                    {goal.isCompleted ? t('goals.reopen') : t('goals.complete')}
                  </button>
                  <button type="button" className="btn btn-outline-danger btn-sm" disabled={change.busy} onClick={() => remove(goal)}
                    aria-label={t('goals.deleteLabel', { title: goal.title })}>×</button>
                </div>
              </div>
            ))}
          </div></div>
        </div>
      </div>
    </>
  );
}
