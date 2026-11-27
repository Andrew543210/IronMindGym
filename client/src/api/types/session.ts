import type { Language } from '../../i18n/language';

export interface Session {
  id: number;
  username: string;
  email: string;
  timeZone: string;
  /** The athlete's calendar day in their time zone, as yyyy-MM-dd. */
  today: string;
  setupComplete: boolean;
  language: Language | null;
}
