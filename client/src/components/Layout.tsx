import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router';
import { errorMessages } from '../api';
import { useAction } from '../hooks';
import { useI18n, type MessageKey } from '../i18n/i18n';
import { useSession } from '../session';
import { LanguageSwitch } from './LanguageSwitch';

const navigation: { to: string; label: MessageKey; number: string }[] = [
  { to: '/', label: 'nav.overview', number: '01' },
  { to: '/workouts', label: 'nav.workout', number: '02' },
  { to: '/exercises', label: 'nav.exercises', number: '03' },
  { to: '/progress', label: 'nav.progress', number: '04' },
  { to: '/nutrition', label: 'nav.nutrition', number: '05' },
  { to: '/statistics', label: 'nav.statistics', number: '06' },
  { to: '/goals', label: 'nav.goals', number: '07' },
  { to: '/awards', label: 'nav.awards', number: '08' },
  { to: '/friends', label: 'nav.friends', number: '09' },
];

const quickLinks: { to: string; label: MessageKey; icon: string }[] = [
  { to: '/', label: 'nav.home', icon: '⌂' },
  { to: '/workouts', label: 'nav.workout', icon: '＋' },
  { to: '/progress', label: 'nav.progress', icon: '↗' },
  { to: '/nutrition', label: 'nav.nutrition', icon: '◉' },
];

interface FlashState {
  message?: string;
  error?: string;
}

/**
 * Shows the message passed with a navigation until the athlete moves to another page. The message is taken out of
 * the history entry, so reloading the page or coming back to it later does not show it again.
 */
function useFlash(): FlashState {
  const location = useLocation();
  const navigate = useNavigate();
  const { message, error, ...rest } = (location.state ?? {}) as FlashState & Record<string, unknown>;
  const incoming = message !== undefined || error !== undefined;
  const page = location.pathname + location.search;
  const [flash, setFlash] = useState<FlashState & { page?: string }>({});

  if (incoming && (message !== flash.message || error !== flash.error || page !== flash.page)) {
    setFlash({ message, error, page });
  } else if (!incoming && flash.page !== undefined && flash.page !== page) {
    setFlash({});
  }

  useEffect(() => {
    if (!incoming) return;
    const { pathname, search, hash } = location;
    void navigate({ pathname, search, hash }, { replace: true, state: Object.keys(rest).length ? rest : null });
  });

  return flash;
}

export function Layout() {
  const { session, signOut } = useSession();
  const { t } = useI18n();
  const [moreOpen, setMoreOpen] = useState(false);
  const flash = useFlash();
  const leaving = useAction();
  const signedIn = session !== null;

  return (
    <div
      className={signedIn ? 'app-shell' : 'public-shell'}
      onKeyDown={(event) => {
        if (event.key === 'Escape') setMoreOpen(false);
      }}
    >
      <a href="#main" className="skip-link">{t('nav.skip')}</a>
      {signedIn && (
        <aside className="sidebar" aria-label={t('nav.main')}>
          <Link className="brand" to="/">
            <img src="/images/logo.jpg" alt="" />
            <span>IRONMIND<small>{t('brand.tagline')}</small></span>
          </Link>
          <p className="nav-caption">{t('nav.caption')}</p>
          <nav>
            {navigation.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.to === '/'} className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}>
                <span className="nav-number">{item.number}</span>{t(item.label)}
              </NavLink>
            ))}
          </nav>
          <div className="sidebar-bottom">
            <p>{t('nav.motto1')}<br /><strong>{t('nav.motto2')}</strong></p>
            <NavLink to="/profile" className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}>
              <span className="nav-number">↗</span>{t('nav.profile')}
            </NavLink>
          </div>
        </aside>
      )}
      <div className="page-shell">
        <header className="topbar">
          {signedIn
            ? <div><span className="status-dot"></span><span className="topbar-label">{t('topbar.journal')}</span></div>
            : <Link className="brand" to="/login">IRONMIND</Link>}
          <div className="d-flex align-items-center gap-3">
            <LanguageSwitch />
            {signedIn ? (
              <>
                <Link className="profile-link" to="/profile">{t('nav.account')}</Link>
                <button type="button" className="btn btn-sm btn-outline-secondary" disabled={leaving.busy}
                  onClick={() => void leaving.run(signOut)}>{t('nav.signOut')}</button>
              </>
            ) : <span className="text-muted small topbar-motto">{t('topbar.public')}</span>}
          </div>
        </header>
        <main id="main" className="main-content">
          {flash.message && <div className="alert alert-success" role="status">{flash.message}</div>}
          {flash.error && <div className="alert alert-danger" role="alert">{flash.error}</div>}
          {signedIn && leaving.error !== undefined && (
            <div className="alert alert-danger" role="alert">{t('nav.signOutFailed')} {errorMessages(leaving.error).join(' ')}</div>
          )}
          <Outlet />
        </main>
        <footer className="page-footer"><span>IRONMIND</span><span>{t('footer.motto')}</span></footer>
      </div>
      {signedIn && (
        <>
          <nav className="mobile-nav" aria-label={t('nav.quick')}>
            {quickLinks.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.to === '/'} onClick={() => setMoreOpen(false)}>
                <b>{item.icon}</b>{t(item.label)}
              </NavLink>
            ))}
            <button type="button" aria-expanded={moreOpen} aria-controls="moreMenu" onClick={() => setMoreOpen((open) => !open)}>
              <b>•••</b>{t('nav.more')}
            </button>
          </nav>
          <div id="moreMenu" className="more-menu" hidden={!moreOpen}>
            {navigation.filter((item) => !quickLinks.some((quick) => quick.to === item.to)).map((item) => (
              <Link key={item.to} to={item.to} onClick={() => setMoreOpen(false)}>{t(item.label)} ↗</Link>
            ))}
            <Link to="/profile" onClick={() => setMoreOpen(false)}>{t('nav.profile')} ↗</Link>
          </div>
        </>
      )}
    </div>
  );
}
