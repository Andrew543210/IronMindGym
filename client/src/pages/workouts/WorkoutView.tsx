import { useState, type FormEvent } from 'react';
import type { ExerciseStat, WorkoutDetails } from '../../api/types/workouts';
import { ErrorAlert, ResponsiveTable } from '../../components/ui';
import { formatNumber, localDateTime } from '../../format';
import { useAction } from '../../hooks';
import { useI18n, type MessageKey } from '../../i18n/i18n';

const MAX_COMMENT_LENGTH = 500;

function recordText(stat: ExerciseStat): MessageKey | null {
  if (!stat.tracksLoad) return null;
  if (stat.isNewRecord) return 'workout.newRecord';
  return stat.heaviestLoadKg === 0 ? 'workout.noLoad' : 'workout.noNewRecord';
}

export function WorkoutSessionView({ workout, timeZone }: { workout: WorkoutDetails; timeZone: string }) {
  const { t } = useI18n();
  const kg = (value: number) => t('unit.kg', { value });
  const sec = (value: number) => t('unit.sec', { value });
  return (
    <>
      <div className="card mb-3"><div className="card-body">
        <h5>{workout.name}</h5>
        <div>{t('workout.date')} <span className="metric">{localDateTime(workout.sessionDate, timeZone)}</span></div>
        <div>{t('workout.duration')} <span className="metric">{t('unit.min', { value: workout.durationMinutes })}</span></div>
        <div>{t('workout.rating')} {workout.rating === null ? '—' : `${workout.rating}/5`}</div>
        <div>{t('workout.notes')} {workout.notes ?? '—'}</div>
      </div></div>

      {workout.exercises.map((performed, index) => {
        const type = performed.exerciseType;
        const hasSetType = performed.sets.some((s) => s.setTypeName?.trim());
        return (
          <div className="card mb-3" key={index}><div className="card-body">
            <h6>{performed.exerciseName}</h6>
            <ResponsiveTable>
              <thead>
                <tr>
                  <th>{t('workout.set')}</th>
                  {hasSetType && <th>{t('workout.type')}</th>}
                  <th>{type === 'StrengthTraining' ? t('workout.load') : t('workout.durationColumn')}</th>
                  <th>{type === 'StrengthTraining' ? t('workout.reps') : type === 'Cardio' ? t('workout.effort') : t('workout.load')}</th>
                  <th>{t('workout.rest')}</th>
                </tr>
              </thead>
              <tbody>
                {performed.sets.map((set) => (
                  <tr key={set.setNumber}>
                    <td>{set.setNumber}</td>
                    {hasSetType && <td>{set.setTypeName ?? ''}</td>}
                    <td className="metric">{type === 'StrengthTraining' ? kg(set.weightKg) : sec(set.durationSeconds)}</td>
                    <td className="metric">{type === 'StrengthTraining' ? set.reps : type === 'Cardio' ? `${set.intensity}/10` : kg(set.weightKg)}</td>
                    <td className="metric">{sec(set.restSeconds)}</td>
                  </tr>
                ))}
              </tbody>
            </ResponsiveTable>
          </div></div>
        );
      })}

      <div className="card border-success mb-3"><div className="card-body">
        <h5>{t('workout.summary')}</h5>
        <p className="mb-1">{t('workout.averageRest')} <strong className="metric">{sec(workout.averageRestSeconds)}</strong></p>
        {workout.remainingWorkoutsThisWeek !== null && (
          <p className="mb-1">{t('workout.left')} <strong className="metric">{workout.remainingWorkoutsThisWeek}</strong></p>
        )}
        <p className="small text-muted mb-3">{t('workout.statsNote')}</p>
        <ResponsiveTable>
          <thead>
            <tr>
              <th>{t('common.exercise')}</th>
              <th>{t('workout.sets')}</th>
              <th>{t('workout.heaviest')}</th>
              <th>{t('workout.versusBest')}</th>
              <th>{t('workout.record')}</th>
            </tr>
          </thead>
          <tbody>
            {workout.exerciseStats.map((stat) => {
              const record = recordText(stat);
              return (
                <tr key={stat.exerciseName}>
                  <td>{stat.exerciseName}</td>
                  <td className="metric">{stat.setsCount}</td>
                  <td className="metric">{stat.tracksLoad ? kg(stat.heaviestLoadKg) : '—'}</td>
                  <td className="metric">{stat.percentOfPreviousBest === null ? '—' : `${formatNumber(stat.percentOfPreviousBest)}%`}</td>
                  <td>{record ? t(record) : '—'}</td>
                </tr>
              );
            })}
          </tbody>
        </ResponsiveTable>
      </div></div>
    </>
  );
}

interface ReactionsProps {
  workout: WorkoutDetails;
  timeZone: string;
  onComment: (text: string) => Promise<void>;
  onLike?: (like: boolean) => Promise<void>;
}

export function WorkoutReactions({ workout, timeZone, onComment, onLike }: ReactionsProps) {
  const { t } = useI18n();
  const [text, setText] = useState('');
  const comment = useAction();
  const like = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    void comment.run(async () => {
      await onComment(text);
      setText('');
    });
  }

  return (
    <div className="card mb-3"><div className="card-body">
      <div className="d-flex align-items-center justify-content-between flex-wrap gap-2 mb-3">
        <div>
          <strong>{t('workout.reactions')}</strong>
          <div className="small">{t('workout.likes')} <span className="metric">{workout.likesCount}</span></div>
        </div>
        {onLike && !workout.isOwner && (
          <button type="button" disabled={like.busy} aria-pressed={workout.isLikedByCurrentUser}
            className={`btn ${workout.isLikedByCurrentUser ? 'btn-light text-dark' : 'btn-outline-light'} btn-sm`}
            onClick={() => void like.run(() => onLike(!workout.isLikedByCurrentUser))}>
            {workout.isLikedByCurrentUser ? t('workout.unlike') : t('workout.like')}
          </button>
        )}
      </div>
      <ErrorAlert error={like.error ?? comment.error} />

      <form className="row g-2" onSubmit={submit}>
        <div className="col-md-10">
          <label htmlFor="commentText" className="visually-hidden">{t('workout.comment')}</label>
          <input type="text" id="commentText" maxLength={MAX_COMMENT_LENGTH} required className="form-control" placeholder={t('workout.commentPlaceholder')}
            value={text} onChange={(e) => setText(e.target.value)} />
        </div>
        <div className="col-md-2">
          <button type="submit" className="btn btn-outline-light w-100" disabled={comment.busy}>{t('workout.post')}</button>
        </div>
      </form>

      <hr />
      {workout.comments.length === 0 && <div className="small text-muted">{t('workout.noComments')}</div>}
      {workout.comments.map((item, index) => (
        <div className="mb-2" key={index}>
          <div><strong>{item.authorUsername}</strong> <span className="small text-muted">{localDateTime(item.createdAt, timeZone)}</span></div>
          <div className="text-break">{item.text}</div>
        </div>
      ))}
    </div></div>
  );
}
