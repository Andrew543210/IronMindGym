export type Language = 'en' | 'uk';

export const LANGUAGES: Language[] = ['en', 'uk'];

const STORAGE_KEY = 'ironmind-language';

export const LOCALES: Record<Language, string> = { en: 'en-US', uk: 'uk-UA' };

function isLanguage(value: unknown): value is Language {
  return value === 'en' || value === 'uk';
}

function detect(): Language {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (isLanguage(saved)) return saved;
  } catch {
    /* Storage may be unavailable; the browser language still works. */
  }
  const preferred = navigator.languages.length ? navigator.languages : [navigator.language];
  return preferred.some((tag) => tag.toLowerCase().startsWith('uk')) ? 'uk' : 'en';
}

let current: Language = detect();
const listeners = new Set<() => void>();

document.documentElement.lang = current;

export function currentLanguage(): Language {
  return current;
}

export function currentLocale(): string {
  return LOCALES[current];
}

export function setLanguage(language: Language) {
  if (language === current) return;
  current = language;
  try {
    localStorage.setItem(STORAGE_KEY, language);
  } catch {
    /* The choice then lasts until the page is reloaded. */
  }
  document.documentElement.lang = language;
  listeners.forEach((listener) => listener());
}

export function subscribeToLanguage(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
