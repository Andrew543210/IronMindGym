import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router';
import { api } from '../api';
import { ErrorAlert, PageState, ResponsiveTable } from '../components/ui';
import { formatNumber, localDateTime, localDay } from '../format';
import { useAction, useApi, usePageTitle } from '../hooks';
import { useI18n, type MessageKey } from '../i18n/i18n';
import { useAthlete } from '../session';
import { WorkoutReactions, WorkoutSessionView, type WorkoutDetails } from './workouts/WorkoutView';

interface FriendCard {
  userId: number;
  username: string;
  isFriend: boolean;
  requestSent: boolean;
  requestReceived: boolean;
  averageKcal: number;
  lastFiveWorkouts: { workoutSessionId: number; name: string; sessionDate: string; durationMinutes: number }[];
  lastNutritionLogs: { date: string; caloriesKcal: number; proteinG: number; fatG: number; carbsG: number }[];
  personalRecords: { exerciseName: string; maxWeightKg: number; updatedAt: string }[];
  achievements: { name: string; conditionDescription: string; achievedAt: string }[];
}

type RequestResult = 'Sent' | 'AlreadyRequested' | 'Connected' | 'AlreadyConnected';

const requestMessages: Record<RequestResult, MessageKey> = {
  Connected: 'friends.connected',
  AlreadyConnected: 'friends.alreadyConnected',
  AlreadyRequested: 'friends.alreadyRequested',
  Sent: 'friends.sent',
};

function statusOf(card: FriendCard): MessageKey {
  if (card.isFriend) return 'friends.statusFriend';
  if (card.requestReceived) return 'friends.statusIncoming';
  return card.requestSent ? 'friends.statusSent' : 'friends.statusNone';
}

export function FriendsPage() {
  const { t } = useI18n();
  usePageTitle(t('friends.title'));
  const athlete = useAthlete();
  const navigate = useNavigate();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const search = params.get('search') ?? '';
  const [query, setQuery] = useState(search);
  const { data, error, loading, reload } = useApi<{ friends: FriendCard[] }>(`/api/friends?search=${encodeURIComponent(search)}`);
  const change = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    setParams(query.trim() ? { search: query.trim() } : {});
  }

  function act(action: () => Promise<MessageKey>) {
    void change.run(async () => {
      const message = await action();
      navigate(location.pathname + location.search, { replace: true, state: { message: t(message) } });
      reload();
    });
  }

  const sendRequest = (card: FriendCard) => act(async () => {
    const { result } = await api.post<{ result: RequestResult }>(`/api/friends/${card.userId}/request`);
    return requestMessages[result];
  });
  const accept = (card: FriendCard) => act(async () => {
    await api.post(`/api/friends/${card.userId}/accept`);
    return 'friends.accepted';
  });
  const remove = (card: FriendCard) => {
    if (card.isFriend && !window.confirm(t('friends.confirmRemove', { name: card.username }))) return;
    act(async () => {
      await api.delete(`/api/friends/${card.userId}`);
      return 'friends.removed';
    });
  };

  return (
    <>
      <h2>{t('friends.heading')}</h2>
      <p className="text-muted">{t('friends.intro')}</p>
      <form className="row g-2 mb-3" role="search" onSubmit={submit}>
        <div className="col-md-4">
          <label htmlFor="searchUsername" className="visually-hidden">{t('common.username')}</label>
          <input id="searchUsername" className="form-control" placeholder={t('friends.searchPlaceholder')} maxLength={50}
            value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
        <div className="col-md-2">
          <button className="btn btn-outline-primary" type="submit">{t('common.search')}</button>
        </div>
      </form>

      <ErrorAlert error={change.error} />
      {!data && <PageState error={error} loading={loading} />}
      {data?.friends.length === 0 && (
        <div className="alert alert-secondary">{search ? t('friends.noMatch', { search }) : t('friends.none')}</div>
      )}

      {data?.friends.map((friend) => (
        <div className="card mb-3" key={friend.userId}><div className="card-body">
          <div>
            <strong className="text-break">{friend.username}</strong>
            <div className="small text-muted">{t('friends.status', { status: t(statusOf(friend)) })}</div>
            {friend.isFriend && (
              <div className="small text-muted">
                {t('friends.avgKcal')} <span className="metric">{formatNumber(friend.averageKcal)}</span> {t('unit.kcalShort')}
              </div>
            )}
          </div>

          <div className="d-flex flex-wrap gap-2 mt-3">
            {friend.requestReceived && (
              <button type="button" className="btn btn-primary btn-sm" disabled={change.busy} onClick={() => accept(friend)}>{t('friends.accept')}</button>
            )}
            {!friend.isFriend && !friend.requestSent && !friend.requestReceived ? (
              <button type="button" className="btn btn-primary btn-sm" disabled={change.busy} onClick={() => sendRequest(friend)}>{t('friends.add')}</button>
            ) : (
              <button type="button" className="btn btn-outline-secondary btn-sm" disabled={change.busy} onClick={() => remove(friend)}>
                {friend.isFriend ? t('friends.remove') : friend.requestSent ? t('friends.cancel') : t('friends.decline')}
              </button>
            )}
          </div>

          {friend.isFriend && (
            <>
              <hr />
              <h6>{t('friends.lastWorkouts')}</h6>
              {friend.lastFiveWorkouts.length === 0 ? <div className="small text-muted">{t('friends.noWorkouts')}</div> : (
                <ul className="mb-0 mt-1">
                  {friend.lastFiveWorkouts.map((workout) => (
                    <li key={workout.workoutSessionId} className="mb-1">
                      <span className="metric">{localDateTime(workout.sessionDate, athlete.timeZone).slice(5)}</span>
                      {' - '}{workout.name} (<span className="metric">{t('unit.min', { value: workout.durationMinutes })}</span>)
                      <Link className="btn btn-outline-light btn-sm ms-2" to={`/friends/${friend.userId}/workouts/${workout.workoutSessionId}`}>{t('common.details')}</Link>
                    </li>
                  ))}
                </ul>
              )}

              <hr />
              <h6>{t('friends.nutrition')}</h6>
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
              <h6>{t('friends.records')}</h6>
              {friend.personalRecords.length === 0 ? <div className="small text-muted">{t('friends.noRecords')}</div> : (
                <ul className="mb-0 mt-1">
                  {friend.personalRecords.map((record) => (
                    <li key={record.exerciseName}>
                      {record.exerciseName}: <span className="metric">{t('unit.kg', { value: record.maxWeightKg })}</span> (<span className="metric">{localDay(record.updatedAt, athlete.timeZone)}</span>)
                    </li>
                  ))}
                </ul>
              )}

              <hr />
              <h6>{t('friends.awards')}</h6>
              {friend.achievements.length === 0 ? <div className="small text-muted">{t('friends.noAwards')}</div> : (
                <ul className="mb-0 mt-1">
                  {friend.achievements.map((award) => (
                    <li key={award.name}>
                      <strong>{award.name}</strong>
                      <span className="small text-muted"> - {award.conditionDescription} ({localDay(award.achievedAt, athlete.timeZone)})</span>
                    </li>
                  ))}
                </ul>
              )}

              <hr />
              <Link to={`/friends/${friend.userId}/progress`} className="btn btn-outline-light btn-sm w-100">{t('friends.viewProgress')}</Link>
            </>
          )}
        </div></div>
      ))}
    </>
  );
}

export function FriendWorkoutPage() {
  const { t } = useI18n();
  usePageTitle(t('friendWorkout.title'));
  const athlete = useAthlete();
  const params = useParams();
  const friendId = Number(params.friendId);
  const sessionId = Number(params.sessionId);
  const route = `/api/friends/${friendId}/workouts/${sessionId}`;
  const { data, error, loading, reload } = useApi<{ friendUsername: string; workout: WorkoutDetails }>(route);

  return (
    <>
      {!data || data.workout.id !== sessionId ? <PageState error={error} loading={loading} /> : (
        <>
          <h2 className="text-break">{t('friendWorkout.heading', { name: data.friendUsername })}</h2>
          <WorkoutSessionView workout={data.workout} timeZone={athlete.timeZone} />
          <WorkoutReactions
            workout={data.workout}
            timeZone={athlete.timeZone}
            onLike={async (liked) => {
              await api.put(`${route}/like`, { liked });
              reload();
            }}
            onComment={async (text) => {
              await api.post(`${route}/comments`, { text });
              reload();
            }}
          />
        </>
      )}
      <div className="mt-3"><Link to="/friends" className="btn btn-outline-light">{t('friends.back')}</Link></div>
    </>
  );
}
