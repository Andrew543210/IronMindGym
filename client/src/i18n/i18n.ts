import { useCallback, useSyncExternalStore } from 'react';
import { numberFormat } from '../format';
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

type Template<K extends MessageKey> = (typeof en)[K] extends string ? (typeof en)[K] : (typeof en)[K][keyof (typeof en)[K]];
type Placeholders<S> = S extends `${string}{${infer Name}}${infer Rest}` ? Name | Placeholders<Rest> : never;
/** The {names} of the English template; plural forms are chosen by count even where they do not show it. */
type ParamNames<K extends MessageKey> = Placeholders<Template<K>> | ((typeof en)[K] extends string ? never : 'count');

/** The parameters a text needs. */
export type ParamsOf<K extends MessageKey> = { [Name in ParamNames<K>]: string | number };

/** No argument for a text without parameters, otherwise exactly its parameters. */
export type ParamArgs<K extends MessageKey> = K extends MessageKey
  ? [ParamNames<K>] extends [never] ? [] : [params: ParamsOf<K>]
  : never;

export type Translate = <K extends MessageKey>(key: K, ...params: ParamArgs<K>) => string;

/** The texts without parameters. */
export type PlainKey = { [K in MessageKey]: [ParamNames<K>] extends [never] ? K : never }[MessageKey];

/** A text to show later, in the language of that moment: a key with exactly the parameters it needs. */
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

export function translateIn<K extends MessageKey>(language: Language, key: K, ...params: ParamArgs<K>): string {
  return render(language, key, (params as [Params?])[0]);
}

/** Words a kept text in the language chosen now, for code that is not a component. */
export function translateText(text: Text): string {
  return render(currentLanguage(), text.key, text.params);
}

/** Translates outside of components, in the language chosen now; components use useI18n, which follows a change. */
export function translate<K extends MessageKey>(key: K, ...params: ParamArgs<K>): string {
  return render(currentLanguage(), key, (params as [Params?])[0]);
}

export function useI18n() {
  const language = useSyncExternalStore(subscribeToLanguage, currentLanguage, currentLanguage);
  const t = useCallback<Translate>((key, ...params) => render(language, key, (params as [Params?])[0]), [language]);
  const show = useCallback((text: Text) => render(language, text.key, text.params), [language]);
  return { language, t, show, setLanguage };
}
