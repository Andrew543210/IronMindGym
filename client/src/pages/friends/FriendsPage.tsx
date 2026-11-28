import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router';
import { api } from '../../api/client';
import type { FriendCard, FriendIndex, FriendRequestResult } from '../../api/types/friends';
import { ErrorAlert, PageState } from '../../components/ui';
import { flash } from '../../flash';
import { useAction, useApi, usePageTitle } from '../../hooks';
import { useI18n, type PlainKey } from '../../i18n/i18n';
import { FriendSummary } from './FriendSummary';

const requestMessages: Record<FriendRequestResult, PlainKey> = {
  Connected: 'friends.connected',
  AlreadyConnected: 'friends.alreadyConnected',
  AlreadyRequested: 'friends.alreadyRequested',
  Sent: 'friends.sent',
};

export function FriendsPage() {
  const { t } = useI18n();
  usePageTitle(t('friends.title'));
  const navigate = useNavigate();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const search = params.get('search') ?? '';
  const [query, setQuery] = useState(search);
  const { data, error, loading, isStale, reload } = useApi<FriendIndex>(
    `/api/friends?search=${encodeURIComponent(search)}`, { keepPrevious: true });
  const change = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    setParams(query.trim() ? { search: query.trim() } : {});
  }

  function act(action: () => Promise<PlainKey>) {
    void change.run(async () => {
      const message = await action();
      void navigate(location.pathname + location.search, { replace: true, state: flash({ key: message }) });
      reload();
    });
  }

  const sendRequest = (card: FriendCard) => act(async () => {
    const { result } = await api.post<{ result: FriendRequestResult }>(`/api/friends/${card.userId}/request`);
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
      <h1 className="h2">{t('friends.heading')}</h1>
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
      {/* The cards of the previous search stay until the new answer comes, without acting on them. */}
      {isStale && <p className="small text-muted" role="status">{t('common.loading')}</p>}
      {data?.friends.length === 0 && !isStale && (
        <div className="alert alert-secondary">{search ? t('friends.noMatch', { search }) : t('friends.none')}</div>
      )}

      <div aria-busy={isStale}>
        {data?.friends.map((friend) => (
          <FriendSummary key={friend.userId} friend={friend} busy={change.busy || isStale} onAccept={accept} onAdd={sendRequest} onRemove={remove} />
        ))}
      </div>
    </>
  );
}
