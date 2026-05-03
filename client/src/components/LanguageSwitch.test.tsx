import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { SessionContext } from '../session';
import { athlete, respond, sessionContext } from '../test/harness';
import { LanguageSwitch } from './LanguageSwitch';

it('switches the interface and saves the choice on the account', async () => {
  const fetchMock = vi.fn().mockResolvedValueOnce(respond(200, { token: 'token-1' })).mockResolvedValueOnce(respond(204));
  vi.stubGlobal('fetch', fetchMock);
  render(<SessionContext.Provider value={sessionContext(athlete)}><LanguageSwitch /></SessionContext.Provider>);

  await userEvent.click(screen.getByRole('button', { name: 'Українська' }));

  expect(screen.getByRole('button', { name: 'Українська' })).toHaveAttribute('aria-pressed', 'true');
  expect(screen.getByRole('group', { name: 'Мова інтерфейсу' })).toBeInTheDocument();
  expect(document.documentElement.lang).toBe('uk');
  await vi.waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  const [url, init] = fetchMock.mock.calls[1] as [string, RequestInit];
  expect(url).toBe('/api/profile/language');
  expect(init.method).toBe('PUT');
  expect(JSON.parse(String(init.body))).toEqual({ language: 'uk' });
});

it('keeps the choice on the device for a visitor', async () => {
  const fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
  render(<SessionContext.Provider value={sessionContext(null)}><LanguageSwitch /></SessionContext.Provider>);

  await userEvent.click(screen.getByRole('button', { name: 'Українська' }));

  expect(localStorage.getItem('ironmind-language')).toBe('uk');
  expect(fetchMock).not.toHaveBeenCalled();
});
