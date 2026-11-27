import { useState, type FormEvent, type ReactNode } from 'react';
import { Link, Navigate, useLocation } from 'react-router';
import { api, FormError } from '../../api/client';
import type { Session } from '../../api/types/session';
import { ErrorAlert } from '../../components/ui';
import { flash } from '../../flash';
import { deviceTimeZone } from '../../format';
import { textFields } from '../../forms';
import { useAction, usePageTitle } from '../../hooks';
import { useI18n, type MessageKey, type Text } from '../../i18n/i18n';
import { useSession, type SessionEnd } from '../../session';

const farewells: Record<SessionEnd, { text: MessageKey; style: string }> = {
  expired: { text: 'errors.sessionEnded', style: 'alert-warning' },
  signedOut: { text: 'nav.signedOut', style: 'alert-success' },
  passwordChanged: { text: 'password.changed', style: 'alert-success' },
};

function AuthLayout({ heading, intro, children }: { heading: string; intro: string; children: ReactNode }) {
  const { t } = useI18n();
  return (
    <div className="auth-layout">
      <div className="auth-story">
        <p className="eyebrow">{t('auth.eyebrow')}</p>
        <h1>{t('auth.story1')}<br />{t('auth.story2')}<br />{t('auth.story3')}</h1>
        <p>{t('auth.storyText')}</p>
      </div>
      <div className="auth-form">
        <p className="eyebrow">{t('auth.formEyebrow')}</p>
        <h2>{heading}</h2>
        <p className="text-muted small mb-4">{intro}</p>
        {children}
      </div>
    </div>
  );
}

export function LoginPage() {
  const { t } = useI18n();
  usePageTitle(t('login.title'));
  const { session, endedBy, signedIn } = useSession();
  const location = useLocation();
  const [form, setForm] = useState({ email: '', password: '', rememberMe: false });
  const [welcome, setWelcome] = useState<Text>();
  const { busy, error, run } = useAction();
  const returnTo = (location.state as { from?: string } | null)?.from;
  if (session) {
    return <Navigate to={session.setupComplete ? (returnTo ?? '/') : '/setup'} replace state={welcome ? flash(welcome) : undefined} />;
  }
  const notice = endedBy ? farewells[endedBy] : undefined;

  function submit(event: FormEvent) {
    event.preventDefault();
    void run(async () => {
      const signedInSession = await api.post<Session>('/api/session/login', form);
      setWelcome({ key: 'login.welcome', params: { name: signedInSession.username } });
      signedIn(signedInSession);
    });
  }

  const field = textFields(form, setForm);

  return (
    <AuthLayout heading={t('login.heading')} intro={t('login.intro')}>
      {notice && !error && <div className={`alert ${notice.style}`} role="status">{t(notice.text)}</div>}
      <form className="row g-3" onSubmit={submit}>
        <ErrorAlert error={error} />
        <div className="col-md-6">
          <label htmlFor="email" className="form-label">{t('common.email')}</label>
          <input {...field('email')} type="email" className="form-control" autoComplete="email" required maxLength={120} />
        </div>
        <div className="col-md-6">
          <label htmlFor="password" className="form-label">{t('common.password')}</label>
          <input {...field('password')} type="password" className="form-control" autoComplete="current-password" required maxLength={128} />
        </div>
        <div className="col-12 form-check">
          <input id="rememberMe" type="checkbox" className="form-check-input"
            checked={form.rememberMe} onChange={(e) => setForm({ ...form, rememberMe: e.target.checked })} />
          <label htmlFor="rememberMe" className="form-check-label">{t('login.remember')}</label>
        </div>
        <div className="col-12 d-flex flex-wrap gap-2">
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t('login.submitting') : t('login.submit')}</button>
          <Link to="/register" className="btn btn-outline-secondary">{t('login.register')}</Link>
        </div>
      </form>
    </AuthLayout>
  );
}

export function RegisterPage() {
  const { t } = useI18n();
  usePageTitle(t('register.title'));
  const { session, signedIn } = useSession();
  const [form, setForm] = useState({ username: '', email: '', age: '', password: '', confirmPassword: '' });
  const [created, setCreated] = useState(false);
  const { busy, error, run, setError } = useAction();
  if (session) {
    return <Navigate to={session.setupComplete ? '/' : '/setup'} replace state={created ? flash({ key: 'register.done' }) : undefined} />;
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    if (form.password !== form.confirmPassword) {
      setError(new FormError(t('register.mismatch')));
      return;
    }
    void run(async () => {
      const account = await api.post<Session>('/api/session/register', {
        ...form,
        age: Number(form.age),
        timeZone: deviceTimeZone() || null,
      });
      setCreated(true);
      signedIn(account);
    });
  }

  const field = textFields(form, setForm);

  return (
    <AuthLayout heading={t('register.heading')} intro={t('register.intro')}>
      <form className="row g-3" onSubmit={submit}>
        <ErrorAlert error={error} />
        <div className="col-md-6">
          <label htmlFor="username" className="form-label">{t('common.username')}</label>
          <input {...field('username')} className="form-control" autoComplete="username" required minLength={2} maxLength={50}
            pattern="[A-Za-z0-9_.\-]{2,50}" aria-describedby="usernameHint" />
          <div id="usernameHint" className="form-text">{t('register.usernameHint')}</div>
        </div>
        <div className="col-md-6">
          <label htmlFor="email" className="form-label">{t('common.email')}</label>
          <input {...field('email')} type="email" className="form-control" autoComplete="email" required maxLength={120} />
        </div>
        <div className="col-md-4">
          <label htmlFor="age" className="form-label">{t('register.age')}</label>
          <input {...field('age')} type="number" inputMode="numeric" className="form-control" required min={12} max={100} />
        </div>
        <div className="col-md-4">
          <label htmlFor="password" className="form-label">{t('common.password')}</label>
          <input {...field('password')} type="password" className="form-control" autoComplete="new-password" required minLength={10} maxLength={128}
            aria-describedby="passwordHint" />
          <div id="passwordHint" className="form-text">{t('register.passwordHint')}</div>
        </div>
        <div className="col-md-4">
          <label htmlFor="confirmPassword" className="form-label">{t('register.confirm')}</label>
          <input {...field('confirmPassword')} type="password" className="form-control" autoComplete="new-password" required maxLength={128} />
        </div>
        <div className="col-12 d-flex flex-wrap gap-2">
          <button type="submit" className="btn btn-success" disabled={busy}>{busy ? t('register.submitting') : t('register.submit')}</button>
          <Link to="/login" className="btn btn-outline-secondary">{t('register.back')}</Link>
        </div>
      </form>
    </AuthLayout>
  );
}
