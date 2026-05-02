import { currentLocale } from './i18n/language';

/**
 * Dates follow the athlete's calendar: moments (when a workout was saved) are shown in the profile's
 * time zone, and calendar days (a weigh-in date, a goal deadline) are shown exactly as stored.
 */

export function calendarDay(value: string): string {
  return value.slice(0, 10);
}

/** The API writes moments in UTC, sometimes with more than three decimals or without the trailing Z. */
export function parseMoment(value: string): Date {
  const trimmed = value.replace(/(\.\d{3})\d+/, '$1');
  return new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(trimmed) ? trimmed : `${trimmed}Z`);
}

type Part = 'year' | 'month' | 'day' | 'hour' | 'minute' | 'weekday';

function parts(date: Date, timeZone: string, options: Intl.DateTimeFormatOptions): Partial<Record<Part, string>> {
  const result: Partial<Record<Part, string>> = {};
  for (const part of new Intl.DateTimeFormat('en-US', { timeZone, ...options }).formatToParts(date))
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
  return new Intl.DateTimeFormat(currentLocale(), { timeZone, ...options }).format(date);
}

export function formatDay(day: string, options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric', year: 'numeric' }): string {
  // Noon UTC keeps the same day in every time zone used for formatting.
  return new Intl.DateTimeFormat(currentLocale(), { timeZone: 'UTC', ...options }).format(new Date(`${calendarDay(day)}T12:00:00Z`));
}

export function formatNumber(value: number, decimals = 1): string {
  return new Intl.NumberFormat(currentLocale(), { maximumFractionDigits: decimals, useGrouping: false }).format(value);
}

export function formatFixed(value: number, decimals: number): string {
  return new Intl.NumberFormat(currentLocale(), {
    minimumFractionDigits: decimals, maximumFractionDigits: decimals, useGrouping: false,
  }).format(value);
}

export function deviceTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || '';
  } catch {
    return '';
  }
}
