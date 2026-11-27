import { Link, useParams } from 'react-router';
import type { FriendProgress } from '../../api/types/progress';
import { PageState } from '../../components/ui';
import { formatFixed } from '../../format';
import { useApi, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { ProgressGroups } from '../progress/ProgressCards';

export function FriendProgressPage() {
  const { t } = useI18n();
  usePageTitle(t('friendProgress.title'));
  const friendId = Number(useParams().friendId);
  const { data, error, loading } = useApi<FriendProgress>(
    `/api/friends/${friendId}/progress`);

  return (
    <div className="mt-4">
      {!data ? <PageState error={error} loading={loading} /> : (
        <>
          <h2 className="text-break">{t('friendProgress.heading', { name: data.friendUsername })}</h2>
          <p className="small text-muted">{t('progress.note')}</p>
          <div className="row mb-4"><div className="col-md-6">
            <div className="card border border-light"><div className="card-body">
              <h6 className="text-muted small mb-2">{t('friendProgress.strength')}</h6>
              <h3 className="mb-0">{formatFixed(data.currentStrengthCoefficient, 2)}</h3>
            </div></div>
          </div></div>
          {data.exercises.length === 0
            ? <div className="alert alert-info">{t('friendProgress.empty', { name: data.friendUsername })}</div>
            : <ProgressGroups entries={data.exercises} />}
        </>
      )}
      <div className="mt-4"><Link to="/friends" className="btn btn-outline-light">{t('friends.back')}</Link></div>
    </div>
  );
}
