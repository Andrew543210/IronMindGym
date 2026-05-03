import { describe, expect, it } from 'vitest';
import { calendarDay, formatDay, formatNumber, localDateTime, localDay, parseMoment } from './format';

describe('dates follow the athlete calendar', () => {
  it('keeps a calendar day as stored, whatever the time zone', () => {
    expect(calendarDay('2026-11-17T00:00:00Z')).toBe('2026-11-17');
    expect(formatDay('2026-11-17T00:00:00Z')).toBe('Nov 17, 2026');
  });

  it('reads UTC moments with microseconds or without a trailing Z', () => {
    expect(parseMoment('2026-11-16T15:05:46.119815Z').toISOString()).toBe('2026-11-16T15:05:46.119Z');
    expect(parseMoment('2026-11-16T15:05:46').toISOString()).toBe('2026-11-16T15:05:46.000Z');
  });

  it('shows a late-evening UTC workout on the next day in Kyiv', () => {
    expect(localDateTime('2026-11-16T22:30:00Z', 'Europe/Kyiv')).toBe('2026-11-17 00:30');
    expect(localDay('2026-11-16T22:30:00Z', 'Europe/Kyiv')).toBe('2026-11-17');
    expect(localDay('2026-11-16T22:30:00Z', 'America/New_York')).toBe('2026-11-16');
  });
});

describe('numbers', () => {
  it('drops trailing zeros like .NET "0.#"', () => {
    expect(formatNumber(75)).toBe('75');
    expect(formatNumber(75.5)).toBe('75.5');
    expect(formatNumber(1234.56)).toBe('1234.6');
  });
});
