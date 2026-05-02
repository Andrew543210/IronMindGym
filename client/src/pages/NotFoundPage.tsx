import { Link } from 'react-router';
import { usePageTitle } from '../hooks';
import { useI18n } from '../i18n/i18n';

export function NotFoundPage() {
  const { t } = useI18n();
  usePageTitle(t('notFound.title'));
  return (
    <div className="empty-state">
      <strong>{t('notFound.text')}</strong>
      <Link to="/">{t('notFound.back')}</Link>
    </div>
  );
}
