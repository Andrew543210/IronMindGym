import { useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router';
import { useI18n, type MessageKey } from '../i18n/i18n';
import { useSession } from '../session';
import { LanguageSwitch } from './LanguageSwitch';

const navigation: { to: string; label: MessageKey; number: string }[] = [
  { to: '/', label: 'nav.overview', number: '01' },
  { to: '/workouts', label: 'nav.workout', number: '02' },
  { to: '/exercises', label: 'nav.exercises', number: '03' },
  { to: '/progress', label: 'nav.progress', number: '04' },
  { to: '/goals', label: 'nav.goals', number: '05' },
  { to: '/friends', label: 'nav.friends', number: '06' },
];

const quickLinks: { to: string; label: MessageKey; icon: string }[] = [
  { to: '/', label: 'nav.home', icon: '⌂' },
  { to: '/workouts', label: 'nav.workout', icon: '＋' },
  { to: '/progress', label: 'nav.progress', icon: '↗' },
];

interface FlashState {
  message?: string;
  error?: string;
}

export function Layout() {
  const { session, signOut } = useSession();
  const { t } = useI18n();
  const [moreOpen, setMoreOpen] = useState(false);
  const location = useLocation();
  const flash = (location.state ?? {}) as FlashState;
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
            {signedIn
              ? <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => void signOut()}>{t('nav.signOut')}</button>
              : <span className="text-muted small topbar-motto">{t('topbar.public')}</span>}
          </div>
        </header>
        <main id="main" className="main-content">
          {flash.message && <div className="alert alert-success" role="status">{flash.message}</div>}
          {flash.error && <div className="alert alert-danger" role="alert">{flash.error}</div>}
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
          </div>
        </>
      )}
    </div>
  );
}
