import type { ReactNode } from 'react';
import { errorMessages } from '../api/client';
import { useI18n } from '../i18n/i18n';

export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null;
  const messages = errorMessages(error);
  return (
    <div className="alert alert-danger" role="alert">
      {messages.length === 1 ? messages[0] : (
        <ul className="mb-0">
          {messages.map((message) => <li key={message}>{message}</li>)}
        </ul>
      )}
    </div>
  );
}

export function PageLoading() {
  const { t } = useI18n();
  return <p className="text-muted" role="status">{t('common.loading')}</p>;
}

export function PageState({ error, loading }: { error: unknown; loading: boolean }) {
  if (error) return <ErrorAlert error={error} />;
  if (loading) return <PageLoading />;
  return null;
}

export function EmptyState({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {children}
    </div>
  );
}

export function ResponsiveTable({ children, className = 'table table-sm' }: { children: ReactNode; className?: string }) {
  return (
    <div className="table-responsive">
      <table className={className}>{children}</table>
    </div>
  );
}
