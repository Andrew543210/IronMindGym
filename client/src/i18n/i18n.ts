import { useCallback, useSyncExternalStore } from 'react';
import { numberFormat } from '../format';
import { en } from './en';
import { currentLanguage, LOCALES, setLanguage, subscribeToLanguage, type Language } from './language';
import { uk } from './uk';

// A plural text picks its form by {count}: English has one and other, Ukrainian one (1, 21), few (2–4),
// many (5–20) and other (fractions).
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

export type Translation = { [K in MessageKey]: (typeof en)[K] extends string ? string : UkrainianPlural };

export type Params = Record<string, string | number>;

type Template<K extends MessageKey> = (typeof en)[K] extends string ? (typeof en)[K] : (typeof en)[K][keyof (typeof en)[K]];
type Placeholders<S> = S extends `${string}{${infer Name}}${infer Rest}` ? Name | Placeholders<Rest> : never;
// A plural text needs {count} even where a form does not show it.
type ParamNames<K extends MessageKey> = Placeholders<Template<K>> | ((typeof en)[K] extends string ? never : 'count');

export type ParamsOf<K extends MessageKey> = { [Name in ParamNames<K>]: string | number };

export type ParamArgs<K extends MessageKey> = K extends MessageKey
  ? [ParamNames<K>] extends [never] ? [] : [params: ParamsOf<K>]
  : never;

export type Translate = <K extends MessageKey>(key: K, ...params: ParamArgs<K>) => string;

export type PlainKey = { [K in MessageKey]: [ParamNames<K>] extends [never] ? K : never }[MessageKey];

// A text to word later, in the language of that moment.
export type Text =
  | { key: PlainKey; params?: undefined }
  | { [K in Exclude<MessageKey, PlainKey>]: { key: K; params: ParamsOf<K> } }[Exclude<MessageKey, PlainKey>];

const dictionaries: Record<Language, Record<MessageKey, string | EnglishPlural | UkrainianPlural>> = { en, uk };

const pluralRules = new Map<Language, Intl.PluralRules>();

function pluralForm(language: Language, count: number): keyof UkrainianPlural {
  let rules = pluralRules.get(language);
  if (!rules) pluralRules.set(language, (rules = new Intl.PluralRules(LOCALES[language])));
  return rules.select(count) as keyof UkrainianPlural;
}

function formatValue(value: string | number, language: Language): string {
  return typeof value === 'number'
    ? numberFormat(LOCALES[language], { maximumFractionDigits: 2, useGrouping: false }).format(value)
    : value;
}

function render(language: Language, key: MessageKey, params?: Params): string {
  const message = dictionaries[language][key];
  let text: string;
  if (typeof message === 'string') {
    text = message;
  } else {
    const count = Number(params?.count ?? 0);
    text = (message as Partial<UkrainianPlural>)[pluralForm(language, count)] ?? message.other;
  }
  if (!params) return text;
  return text.replace(/\{(\w+)\}/g, (match, name: string) => {
    const value = params[name];
    return value === undefined ? match : formatValue(value, language);
  });
}

export function translateText(text: Text): string {
  return render(currentLanguage(), text.key, text.params);
}

// For code outside components; components use useI18n, which follows a change of language.
export function translate<K extends MessageKey>(key: K, ...params: ParamArgs<K>): string {
  return render(currentLanguage(), key, (params as [Params?])[0]);
}

export function useI18n() {
  const language = useSyncExternalStore(subscribeToLanguage, currentLanguage, currentLanguage);
  const t = useCallback<Translate>((key, ...params) => render(language, key, (params as [Params?])[0]), [language]);
  const show = useCallback((text: Text) => render(language, text.key, text.params), [language]);
  return { language, t, show, setLanguage };
}
