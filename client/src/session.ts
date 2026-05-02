import type { Language } from './i18n/language';
import { createContext, useContext } from 'react';

export interface Session {
  id: number;
  username: string;
  email: string;
  timeZone: string;
  today: string;
  setupComplete: boolean;
  language: Language | null;
}

/** Why the last session closed; the login page explains it once. */
export type SessionEnd = 'expired' | 'signedOut' | 'passwordChanged';

export interface SessionState {
  loaded: boolean;
  session: Session | null;
  endedBy: SessionEnd | null;
}

export interface SessionContextValue extends SessionState {
  signedIn(session: Session): void;
  /** The server already ended the session, for example after a password change. */
  signedOut(reason: SessionEnd): void;
  signOut(): Promise<void>;
  refresh(): Promise<void>;
}

export const SessionContext = createContext<SessionContextValue | null>(null);

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext);
  if (!value) throw new Error('useSession must be used inside SessionProvider.');
  return value;
}

export function useAthlete(): Session {
  const { session } = useSession();
  if (!session) throw new Error('useAthlete must be used on a page that requires a session.');
  return session;
}
