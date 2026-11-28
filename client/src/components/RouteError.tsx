import { Link, useRouteError } from 'react-router';
import { usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';

// A tab opened before a release asks for bundles the release removed; each browser words that failure differently.
function isMissingBundle(error: unknown): boolean {
  return error instanceof TypeError && /dynamically imported module|importing a module script failed/i.test(error.message);
}

export function RouteError() {
  const error = useRouteError();
  const { t } = useI18n();
  const outdated = isMissingBundle(error);
  usePageTitle(t(outdated ? 'routeError.updatedTitle' : 'routeError.title'));
  return (
    <div className="empty-state" role="alert">
      <h1 className="empty-state-title">{t(outdated ? 'routeError.updated' : 'routeError.unexpected')}</h1>
      <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>{t('routeError.reload')}</button>
      <Link to="/">{t('notFound.back')}</Link>
    </div>
  );
}
