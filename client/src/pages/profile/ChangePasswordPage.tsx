import { useState, type FormEvent } from 'react';
import { api, FormError } from '../../api/client';
import { ErrorAlert } from '../../components/ui';
import { textFields } from '../../forms';
import { useAction, usePageTitle } from '../../hooks';
import { useI18n } from '../../i18n/i18n';
import { useSession } from '../../session';

export function ChangePasswordPage() {
  const { t } = useI18n();
  usePageTitle(t('password.title'));
  const { signedOut } = useSession();
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const { busy, error, run, setError } = useAction();

  function submit(event: FormEvent) {
    event.preventDefault();
    if (form.newPassword !== form.confirmPassword) {
      setError(new FormError(t('register.mismatch')));
      return;
    }
    void run(async () => {
      await api.put('/api/profile/password', form);
      signedOut('passwordChanged');
    });
  }

  const field = textFields(form, setForm);
  const input = (name: keyof typeof form, label: string, autoComplete: string) => (
    <div className="col-12">
      <label htmlFor={name} className="form-label">{label}</label>
      <input type="password" className="form-control" required minLength={name === 'currentPassword' ? undefined : 10} maxLength={128}
        autoComplete={autoComplete} {...field(name)} />
    </div>
  );

  return (
    <>
      <p className="eyebrow">{t('password.eyebrow')}</p>
      <h1 className="h2">{t('password.title')}</h1>
      <div className="card narrow-card"><div className="card-body">
        <p className="text-muted small">{t('password.hint')}</p>
        <form className="row g-3" onSubmit={submit}>
          <ErrorAlert error={error} />
          {input('currentPassword', t('password.current'), 'current-password')}
          {input('newPassword', t('password.new'), 'new-password')}
          {input('confirmPassword', t('password.confirm'), 'new-password')}
          <div className="col-12">
            <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t('common.saving') : t('password.submit')}</button>
          </div>
        </form>
      </div></div>
    </>
  );
}
