import type { Language } from '../../i18n/language';

export interface Session {
  id: number;
  username: string;
  email: string;
  timeZone: string;
  today: string;
  setupComplete: boolean;
  language: Language | null;
}
