import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, ApiError, forgetCsrfToken, whenSessionEnds } from './api';
import { currentLanguage, setLanguage } from './i18n/language';
import { SessionContext, type Session, type SessionContextValue, type SessionEnd, type SessionState } from './session';

/**
 * The account's saved language wins after sign-in, so every device shows the same one.
 * An account without a choice yet keeps the language of this device.
 */
function adoptLanguage(session: Session | null | undefined) {
  if (!session) return;
  if (session.language) setLanguage(session.language);
  else api.put('/api/profile/language', { language: currentLanguage() }).catch(() => undefined);
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SessionState>({ loaded: false, session: null, endedBy: null });

  useEffect(() => {
    whenSessionEnds(() => {
      forgetCsrfToken();
      setState({ loaded: true, session: null, endedBy: 'expired' });
    });
    let active = true;
    api.get<Session | undefined>('/api/session').then(
      (session) => {
        if (!active) return;
        adoptLanguage(session);
        setState({ loaded: true, session: session ?? null, endedBy: null });
      },
      () => active && setState((current) => ({ ...current, loaded: true })),
    );
    return () => {
      active = false;
    };
  }, []);

  const refresh = useCallback(async () => {
    const session = await api.get<Session | undefined>('/api/session');
    setState({ loaded: true, session: session ?? null, endedBy: null });
  }, []);

  const value = useMemo<SessionContextValue>(() => {
    const signedOut = (reason: SessionEnd) => {
      forgetCsrfToken();
      setState({ loaded: true, session: null, endedBy: reason });
    };
    return {
      ...state,
      signedIn(session) {
        forgetCsrfToken();
        adoptLanguage(session);
        setState({ loaded: true, session, endedBy: null });
      },
      signedOut,
      async signOut() {
        try {
          await api.post('/api/session/logout');
        } catch (failure) {
          // A session that has already ended needs no sign-out; any other failure leaves the athlete signed in.
          if (!(failure instanceof ApiError && failure.status === 401)) throw failure;
        }
        signedOut('signedOut');
      },
      refresh,
    };
  }, [state, refresh]);

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}
