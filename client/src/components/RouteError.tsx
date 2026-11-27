import { isRouteErrorResponse, Link, useRouteError } from 'react-router';
import { usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';
import { NotFoundPage } from '../pages/NotFoundPage';

/**
 * A section bundle the server no longer has. A tab opened before a release asks for the file names of the old
 * build, which the release removed; each browser words the failed import differently.
 */
function isMissingBundle(error: unknown): boolean {
  return error instanceof TypeError && /dynamically imported module|importing a module script failed/i.test(error.message);
}

/** Replaces a page that failed to load or render; the layout and its navigation stay usable. */
export function RouteError() {
  const error = useRouteError();
  const { t } = useI18n();
  const outdated = isMissingBundle(error);
  usePageTitle(t(outdated ? 'routeError.updatedTitle' : 'routeError.title'));
  if (isRouteErrorResponse(error) && error.status === 404) return <NotFoundPage />;
  return (
    <div className="empty-state" role="alert">
      <strong>{t(outdated ? 'routeError.updated' : 'routeError.unexpected')}</strong>
      <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>{t('routeError.reload')}</button>
      <Link to="/">{t('notFound.back')}</Link>
    </div>
  );
}
