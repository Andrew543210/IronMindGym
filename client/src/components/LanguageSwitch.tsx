import { api } from '../api/client';
import { useI18n } from '../i18n/i18n';
import { LANGUAGES, type Language } from '../i18n/language';
import { useSession } from '../session';

const NAMES: Record<Language, { short: string; full: string }> = {
  en: { short: 'EN', full: 'English' },
  uk: { short: 'УКР', full: 'Українська' },
};

export function LanguageSwitch() {
  const { language, setLanguage, t } = useI18n();
  const { session } = useSession();

  function choose(next: Language) {
    if (next === language) return;
    setLanguage(next);
    // The choice already applies on this device; if saving fails, the account keeps the old one
    // until the next change, which is not worth an error message.
    if (session) api.put('/api/profile/language', { language: next }).catch(() => undefined);
  }

  return (
    <div className="language-switch" role="group" aria-label={t('language.label')}>
      {LANGUAGES.map((code) => (
        <button key={code} type="button" lang={code} aria-pressed={code === language} aria-label={NAMES[code].full}
          className={code === language ? 'active' : undefined} onClick={() => choose(code)}>
          {NAMES[code].short}
        </button>
      ))}
    </div>
  );
}
