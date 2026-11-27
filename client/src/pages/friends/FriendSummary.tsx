import { Link } from 'react-router';
import type { FriendCard } from '../../api/types/friends';
import { Phrase } from '../../components/Phrase';
import { ResponsiveTable } from '../../components/ui';
import { formatNumber, localDateTime, localDay } from '../../format';
import { useI18n, type MessageKey } from '../../i18n/i18n';
import { useAthlete } from '../../session';

function statusOf(card: FriendCard): MessageKey {
  if (card.isFriend) return 'friends.statusFriend';
  if (card.requestReceived) return 'friends.statusIncoming';
  return card.requestSent ? 'friends.statusSent' : 'friends.statusNone';
}

interface FriendSummaryProps {
  friend: FriendCard;
  busy: boolean;
  onAccept: (friend: FriendCard) => void;
  onAdd: (friend: FriendCard) => void;
  onRemove: (friend: FriendCard) => void;
}

/** A connection or a search result: the status, the actions, and what a friend shares. */
export function FriendSummary({ friend, busy, onAccept, onAdd, onRemove }: FriendSummaryProps) {
  const { t } = useI18n();
  const athlete = useAthlete();
  return (
    <div className="card mb-3"><div className="card-body">
      <div>
        <h2 className="fs-6 fw-bold text-break mb-0">{friend.username}</h2>
        <div className="small text-muted">{t('friends.status', { status: t(statusOf(friend)) })}</div>
        {friend.isFriend && (
          <div className="small text-muted">
            <Phrase text="friends.avgKcal" parts={{ kcal: <span className="metric">{formatNumber(friend.averageKcal)}</span> }} />
          </div>
        )}
      </div>

      <div className="d-flex flex-wrap gap-2 mt-3">
        {friend.requestReceived && (
          <button type="button" className="btn btn-primary btn-sm" disabled={busy} onClick={() => onAccept(friend)}>{t('friends.accept')}</button>
        )}
        {!friend.isFriend && !friend.requestSent && !friend.requestReceived ? (
          <button type="button" className="btn btn-primary btn-sm" disabled={busy} onClick={() => onAdd(friend)}>{t('friends.add')}</button>
        ) : (
          <button type="button" className="btn btn-outline-secondary btn-sm" disabled={busy} onClick={() => onRemove(friend)}>
            {friend.isFriend ? t('friends.remove') : friend.requestSent ? t('friends.cancel') : t('friends.decline')}
          </button>
        )}
      </div>

      {friend.isFriend && (
        <>
          <hr />
          <h3 className="h6">{t('friends.lastWorkouts')}</h3>
          {friend.lastFiveWorkouts.length === 0 ? <div className="small text-muted">{t('friends.noWorkouts')}</div> : (
            <ul className="mb-0 mt-1">
              {friend.lastFiveWorkouts.map((workout) => (
                <li key={workout.workoutSessionId} className="mb-1">
                  <Phrase text="workouts.entry" parts={{
                    date: <span className="metric">{localDateTime(workout.sessionDate, athlete.timeZone).slice(5)}</span>,
                    name: workout.name,
                    duration: <span className="metric">{t('unit.min', { value: workout.durationMinutes })}</span>,
                  }} />
                  <Link className="btn btn-outline-light btn-sm ms-2" to={`/friends/${friend.userId}/workouts/${workout.workoutSessionId}`}>{t('common.details')}</Link>
                </li>
              ))}
            </ul>
          )}

          <hr />
          <h3 className="h6">{t('friends.nutrition')}</h3>
          {friend.lastNutritionLogs.length === 0 ? <div className="small text-muted">{t('friends.noNutrition')}</div> : (
            <ResponsiveTable>
              <thead>
                <tr>
                  <th>{t('common.date')}</th>
                  <th>{t('macro.kcal')}</th>
                  <th><abbr title={t('macro.protein')}>{t('macro.p')}</abbr></th>
                  <th><abbr title={t('macro.fat')}>{t('macro.f')}</abbr></th>
                  <th><abbr title={t('macro.carbs')}>{t('macro.c')}</abbr></th>
                </tr>
              </thead>
              <tbody>
                {friend.lastNutritionLogs.map((log) => (
                  <tr key={log.date}>
                    <td className="metric">{log.date}</td>
                    <td className="metric">{formatNumber(log.caloriesKcal)}</td>
                    <td className="metric">{formatNumber(log.proteinG)}</td>
                    <td className="metric">{formatNumber(log.fatG)}</td>
                    <td className="metric">{formatNumber(log.carbsG)}</td>
                  </tr>
                ))}
              </tbody>
            </ResponsiveTable>
          )}

          <hr />
          <h3 className="h6">{t('friends.records')}</h3>
          {friend.personalRecords.length === 0 ? <div className="small text-muted">{t('friends.noRecords')}</div> : (
            <ul className="mb-0 mt-1">
              {friend.personalRecords.map((record) => (
                <li key={record.exerciseName}>
                  <Phrase text="friends.record" parts={{
                    exercise: record.exerciseName,
                    weight: <span className="metric">{t('unit.kg', { value: record.maxWeightKg })}</span>,
                    date: <span className="metric">{localDay(record.achievedAt, athlete.timeZone)}</span>,
                  }} />
                </li>
              ))}
            </ul>
          )}

          <hr />
          <h3 className="h6">{t('friends.awards')}</h3>
          {friend.achievements.length === 0 ? <div className="small text-muted">{t('friends.noAwards')}</div> : (
            <ul className="mb-0 mt-1">
              {friend.achievements.map((award) => (
                <li key={award.name}>
                  <Phrase text="friends.award" parts={{
                    name: <strong>{award.name}</strong>,
                    condition: <span className="small text-muted">{award.conditionDescription}</span>,
                    date: <span className="small text-muted">{localDay(award.achievedAt, athlete.timeZone)}</span>,
                  }} />
                </li>
              ))}
            </ul>
          )}

          <hr />
          <Link to={`/friends/${friend.userId}/progress`} className="btn btn-outline-light btn-sm w-100">{t('friends.viewProgress')}</Link>
        </>
      )}
    </div></div>
  );
}
