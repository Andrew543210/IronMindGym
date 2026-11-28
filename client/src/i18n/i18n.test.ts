import { describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import { en } from './en';
import { translate, type MessageKey } from './i18n';
import { currentLanguage, setLanguage } from './language';
import { uk } from './uk';

const keys = Object.keys(en) as MessageKey[];

function parameters(message: string | object): string[] {
  const forms = typeof message === 'string' ? [message] : Object.values(message as Record<string, string>);
  return [...new Set(forms.flatMap((form) => [...form.matchAll(/\{(\w+)\}/g)].flatMap((match) => (match[1] ? [match[1]] : []))))].sort();
}

describe('dictionaries', () => {
  it('have the same texts in both languages', () => {
    expect(Object.keys(uk).sort()).toEqual([...keys].sort());
  });

  it('fill every text and every Ukrainian plural form', () => {
    for (const key of keys) {
      const message = uk[key];
      const forms = typeof message === 'string' ? [message] : [message.one, message.few, message.many, message.other];
      for (const form of forms) expect(form.trim(), key).not.toBe('');
    }
  });

  it('use the same parameters in both languages', () => {
    for (const key of keys) expect(parameters(uk[key]), key).toEqual(parameters(en[key]));
  });

  it('write Ukrainian with the typographic apostrophe', () => {
    for (const key of keys) expect(JSON.stringify(uk[key]), key).not.toMatch(/[а-яіїєґ]'[а-яіїєґ]/i);
  });
});

describe('translate', () => {
  it('picks the Ukrainian plural form by the number', () => {
    setLanguage('uk');
    expect(translate('count.sets', { count: 1 })).toBe('1 підхід');
    expect(translate('count.sets', { count: 3 })).toBe('3 підходи');
    expect(translate('count.sets', { count: 5 })).toBe('5 підходів');
    expect(translate('count.sets', { count: 11 })).toBe('11 підходів');
    expect(translate('count.sets', { count: 21 })).toBe('21 підхід');
    expect(translate('count.sets', { count: 22 })).toBe('22 підходи');
    setLanguage('en');
    expect(translate('count.sets', { count: 1 })).toBe('1 set');
    expect(translate('count.sets', { count: 2 })).toBe('2 sets');
  });

  it('writes numbers with the decimal sign of the language', () => {
    expect(translate('unit.kg', { value: 62.5 })).toBe('62.5 kg');
    setLanguage('uk');
    expect(translate('unit.kg', { value: 62.5 })).toBe('62,5 кг');
    expect(translate('unit.kg', { value: 1200 })).toBe('1200 кг');
  });

  it('follows the chosen language', () => {
    setLanguage('uk');
    expect(currentLanguage()).toBe('uk');
    expect(document.documentElement.lang).toBe('uk');
    expect(translate('common.exercise')).toBe(uk['common.exercise']);
  });
});

describe('requests', () => {
  it('ask the server for names in the interface language', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    setLanguage('uk');
    await api.get('/api/dashboard');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Record<string, string>)['Accept-Language']).toBe('uk');
  });

  it('show connection errors in the interface language', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    setLanguage('uk');
    await expect(api.get('/api/dashboard')).rejects.toThrow(uk['errors.offline']);
  });
});
