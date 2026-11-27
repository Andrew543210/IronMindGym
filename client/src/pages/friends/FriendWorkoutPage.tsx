import { Link, useParams } from 'react-router';
import { api } from '../../api/client';
import type { FriendWorkout } from '../../api/types/friends';
import { PageState } from '../../components/ui';
import { useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useAthlete } from '../../session';
import { WorkoutReactions, WorkoutSessionView } from '../workouts/WorkoutView';

export function FriendWorkoutPage() {
  const { t } = useI18n();
  usePageTitle(t('friendWorkout.title'));
  const athlete = useAthlete();
  const params = useParams();
  const friendId = Number(params.friendId);
  const sessionId = Number(params.sessionId);
  const route = `/api/friends/${friendId}/workouts/${sessionId}`;
  const { data, error, loading, reload } = useApi<FriendWorkout>(route);

  return (
    <>
      {!data ? <PageState error={error} loading={loading} /> : (
        <>
          <h1 className="h2 text-break">{t('friendWorkout.heading', { name: data.friendUsername })}</h1>
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
