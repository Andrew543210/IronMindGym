import { currentLocale } from './i18n/language';

// The API writes moments in UTC, sometimes with more than three decimals or without the trailing Z.
export function parseMoment(value: string): Date {
  const trimmed = value.replace(/(\.\d{3})\d+/, '$1');
  return new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(trimmed) ? trimmed : `${trimmed}Z`);
}

// Each formatter is built once per locale and options: tables format hundreds of cells per render.
function reused<T, O>(create: (locale: string, options: O) => T): (locale: string, options: O) => T {
  const made = new Map<string, T>();
  return (locale, options) => {
    const key = `${locale} ${JSON.stringify(options)}`;
    let value = made.get(key);
    if (value === undefined) made.set(key, (value = create(locale, options)));
    return value;
  };
}

const dateFormat = reused((locale, options: Intl.DateTimeFormatOptions) => new Intl.DateTimeFormat(locale, options));
export const numberFormat = reused((locale, options: Intl.NumberFormatOptions) => new Intl.NumberFormat(locale, options));

type Part = 'year' | 'month' | 'day' | 'hour' | 'minute' | 'weekday';

function parts(date: Date, timeZone: string, options: Intl.DateTimeFormatOptions): Partial<Record<Part, string>> {
  const result: Partial<Record<Part, string>> = {};
  for (const part of dateFormat('en-US', { timeZone, ...options }).formatToParts(date))
    if (part.type !== 'literal') result[part.type as Part] = part.value;
  return result;
}

export function localDateTime(value: string, timeZone: string): string {
  const p = parts(parseMoment(value), timeZone, {
    year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  });
  return `${p.year}-${p.month}-${p.day} ${p.hour}:${p.minute}`;
}

export function localDay(value: string, timeZone: string): string {
  const p = parts(parseMoment(value), timeZone, { year: 'numeric', month: '2-digit', day: '2-digit' });
  return `${p.year}-${p.month}-${p.day}`;
}

export function localFormat(value: string | Date, timeZone: string, options: Intl.DateTimeFormatOptions): string {
  const date = typeof value === 'string' ? parseMoment(value) : value;
  return dateFormat(currentLocale(), { timeZone, ...options }).format(date);
}

export function formatDay(day: string, options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric', year: 'numeric' }): string {
  // Noon UTC keeps the same day in every time zone used for formatting.
  return dateFormat(currentLocale(), { timeZone: 'UTC', ...options }).format(new Date(`${day}T12:00:00Z`));
}

export function formatNumber(value: number, decimals = 1): string {
  return numberFormat(currentLocale(), { maximumFractionDigits: decimals, useGrouping: false }).format(value);
}

export function formatFixed(value: number, decimals: number): string {
  return numberFormat(currentLocale(), { minimumFractionDigits: decimals, maximumFractionDigits: decimals, useGrouping: false }).format(value);
}

export function deviceTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || '';
  } catch {
    return '';
  }
}

export function formatSigned(value: number, decimals = 1): string {
  const rounded = Number(value.toFixed(decimals));
  if (rounded === 0) return '0';
  return `${rounded > 0 ? '+' : ''}${formatNumber(rounded, decimals)}`;
}
