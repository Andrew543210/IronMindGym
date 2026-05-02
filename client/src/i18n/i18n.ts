import { useCallback, useSyncExternalStore } from 'react';
import { en } from './en';
import { currentLanguage, LOCALES, setLanguage, subscribeToLanguage, type Language } from './language';
import { uk } from './uk';

/**
 * Interface texts in English and Ukrainian. A text is either a string or a set of plural forms chosen by
 * the {count} parameter: English has "one" and "other", Ukrainian "one" (1, 21), "few" (2–4, 22–24),
 * "many" (5–20, 25) and "other" (fractions, 1.5). Parameters are written as {name}.
 */
interface EnglishPlural {
  one: string;
  other: string;
}

interface UkrainianPlural {
  one: string;
  few: string;
  many: string;
  other: string;
}

export type MessageKey = keyof typeof en;

/** Every English key must exist in Ukrainian, with plural forms where English has them. */
export type Translation = { [K in MessageKey]: (typeof en)[K] extends string ? string : UkrainianPlural };

export type Params = Record<string, string | number>;

export interface Text {
  key: MessageKey;
  params?: Params;
}

const dictionaries: Record<Language, Record<MessageKey, string | EnglishPlural | UkrainianPlural>> = { en, uk };

const pluralRules = new Map<Language, Intl.PluralRules>();

function pluralForm(language: Language, count: number): keyof UkrainianPlural {
  let rules = pluralRules.get(language);
  if (!rules) pluralRules.set(language, (rules = new Intl.PluralRules(LOCALES[language])));
  return rules.select(count) as keyof UkrainianPlural;
}

function formatValue(value: string | number, language: Language = currentLanguage()): string {
  return typeof value === 'number'
    ? new Intl.NumberFormat(LOCALES[language], { maximumFractionDigits: 2, useGrouping: false }).format(value)
    : value;
}

export function translate(key: MessageKey, params?: Params, language: Language = currentLanguage()): string {
  const message = dictionaries[language][key];
  let text: string;
  if (typeof message === 'string') {
    text = message;
  } else {
    const count = Number(params?.count ?? 0);
    text = (message as Partial<UkrainianPlural>)[pluralForm(language, count)] ?? message.other;
  }
  if (!params) return text;
  return text.replace(/\{(\w+)\}/g, (match, name: string) => (name in params ? formatValue(params[name], language) : match));
}

export function useI18n() {
  const language = useSyncExternalStore(subscribeToLanguage, currentLanguage, currentLanguage);
  const t = useCallback((key: MessageKey, params?: Params) => translate(key, params, language), [language]);
  const show = useCallback((text: Text) => translate(text.key, text.params, language), [language]);
  return { language, t, show, setLanguage };
}

export type Translate = ReturnType<typeof useI18n>['t'];
