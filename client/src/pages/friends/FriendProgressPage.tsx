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
          <h1 className="h2 text-break">{t('friendProgress.heading', { name: data.friendUsername })}</h1>
          <p className="small text-muted">{t('progress.note')}</p>
          <div className="row mb-4"><div className="col-md-6">
            <div className="card"><div className="card-body">
              <h2 className="h6 text-muted small mb-2 text-uppercase">{t('friendProgress.strength')}</h2>
              <p className="h3 mb-0">{formatFixed(data.currentStrengthCoefficient, 2)}</p>
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
