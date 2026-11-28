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

/**
 * What a page shows before its data: the error or the loading note. A page that has no heading of its own
 * until the data comes passes its `title`, so the page always has its h1.
 */
export function PageState({ error, loading, title }: { error: unknown; loading: boolean; title?: string }) {
  const state = error ? <ErrorAlert error={error} /> : loading ? <PageLoading /> : null;
  if (title === undefined) return state;
  return (
    <>
      <h1 className="h2">{title}</h1>
      {state}
    </>
  );
}

export function EmptyState({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {children}
    </div>
  );
}

/** A symbol that only decorates its text, such as an arrow; screen readers skip it. */
export function Glyph({ children }: { children: string }) {
  return <span aria-hidden="true">{children}</span>;
}

export function ResponsiveTable({ children, className = 'table table-sm' }: { children: ReactNode; className?: string }) {
  return (
    <div className="table-responsive">
      <table className={className}>{children}</table>
    </div>
  );
}
