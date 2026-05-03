import { afterEach, describe, expect, it, vi } from 'vitest';

async function detect(browserLanguages: string[], saved?: string) {
  vi.resetModules();
  vi.spyOn(navigator, 'languages', 'get').mockReturnValue(browserLanguages);
  if (saved) localStorage.setItem('ironmind-language', saved);
  const { currentLanguage } = await import('./language');
  return currentLanguage();
}

describe('the first language', () => {
  afterEach(() => {
    document.documentElement.lang = 'en';
  });

  it('follows the browser until the athlete chooses one', async () => {
    expect(await detect(['uk-UA', 'en-US'])).toBe('uk');
    expect(document.documentElement.lang).toBe('uk');
    expect(await detect(['pl-PL', 'en-GB'])).toBe('en');
  });

  it('keeps the choice saved on this device', async () => {
    expect(await detect(['en-US'], 'uk')).toBe('uk');
    expect(await detect(['uk-UA'], 'de')).toBe('uk');
  });

  it('still works when storage is blocked', async () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });
    expect(await detect(['uk'])).toBe('uk');
  });
});
