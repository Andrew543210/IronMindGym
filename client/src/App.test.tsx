import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { App } from './App';
import { draftStore, freshDraft, type DraftSettings } from './pages/workouts/draft';
import { athlete, mockApi, respond } from './test/harness';
import { overview } from './test/fixtures';

const settings: DraftSettings = { splitTypeId: '2', splitDayId: '3', durationMinutes: '60', reviewRating: '4', reviewComment: '' };
const set = { exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 90 };

function open(path: string) {
  window.history.replaceState(null, '', path);
  render(<App />);
}

describe('app', () => {
  it('sends a visitor to sign in and back to the page they asked for', async () => {
    mockApi({
      'GET /api/session': null,
      'POST /api/session/login': athlete,
      'GET /api/goals': [],
    });
    open('/goals');
    const user = userEvent.setup();

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    await user.type(screen.getByLabelText('Email'), 'athlete@example.com');
    await user.type(screen.getByLabelText('Password'), 'TrainingPassword123!');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('Welcome, athlete!')).toBeInTheDocument();
    expect(await screen.findByText('Make your next step clear.')).toBeInTheDocument();
    expect(window.location.pathname).toBe('/goals');
  });

  it('asks a new account to finish the setup and then opens the overview', async () => {
    let setupComplete = false;
    const calls = mockApi({
      'GET /api/session': () => ({ ...athlete, setupComplete }),
      'GET /api/setup': { splitTypes: [{ id: 1, name: 'Full-body' }, { id: 2, name: 'Upper-Lower' }], alreadyCompleted: false },
      'POST /api/setup': () => {
        setupComplete = true;
        return null;
      },
      'GET /api/dashboard': overview,
    });
    open('/goals');
    const user = userEvent.setup();

    expect(await screen.findByRole('heading', { name: 'Initial Setup' })).toBeInTheDocument();
    await user.type(await screen.findByLabelText('Weight (kg)'), '80');
    await user.type(screen.getByLabelText('Height (cm)'), '180');
    await user.selectOptions(screen.getByLabelText('Split type'), 'Upper-Lower');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Initial setup data saved.')).toBeInTheDocument();
    expect(await screen.findByText("Let's build, athlete.")).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'POST')?.body).toMatchObject({ weightKg: 80, heightCm: 180, splitTypeId: 2, workoutsPerWeek: 3 });
  });

  it('returns to the login page with a warning when the session expires', async () => {
    mockApi({ 'GET /api/session': athlete, 'GET /api/goals': () => respond(401) });
    open('/goals');

    expect(await screen.findByText('Your session has ended. Sign in again.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
  });

  it('signs out from the top bar and opens the extra sections on a phone', async () => {
    mockApi({ 'GET /api/session': athlete, 'GET /api/dashboard': overview, 'POST /api/session/logout': null });
    open('/');
    const user = userEvent.setup();

    await screen.findByText("Let's build, athlete.");
    const more = screen.getByRole('button', { name: /More/ });
    await user.click(more);
    expect(more).toHaveAttribute('aria-expanded', 'true');
    expect(within(document.getElementById('moreMenu')!).getByRole('link', { name: 'Goals' })).toBeVisible();
    await user.keyboard('{Escape}');
    expect(more).toHaveAttribute('aria-expanded', 'false');

    await user.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(await screen.findByText('You have logged out.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
  });

  it('removes the workout of the athlete who signed out from this browser once the server holds it', async () => {
    const store = draftStore(athlete.id);
    const draft = { ...freshDraft(settings), sets: [set] };
    store.write(draft);
    store.markSent(draft);
    mockApi({ 'GET /api/session': athlete, 'GET /api/dashboard': overview, 'POST /api/session/logout': null });
    open('/');
    const user = userEvent.setup();

    await screen.findByText("Let's build, athlete.");
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByText('You have logged out.')).toBeInTheDocument();
    expect(store.read()).toBeNull();
  });

  it('keeps a workout the server has not received after the sign-out', async () => {
    const store = draftStore(athlete.id);
    store.write({ ...freshDraft(settings), sets: [set] });
    mockApi({ 'GET /api/session': athlete, 'GET /api/dashboard': overview, 'POST /api/session/logout': null });
    open('/');
    const user = userEvent.setup();

    await screen.findByText("Let's build, athlete.");
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByText('You have logged out.')).toBeInTheDocument();
    expect(store.read()?.sets).toEqual([set]);
  });

  it('says so when the sign-out fails and keeps the athlete signed in', async () => {
    let online = false;
    mockApi({
      'GET /api/session': athlete,
      'GET /api/dashboard': overview,
      'POST /api/session/logout': () => (online ? null : respond(503)),
      'GET /api/goals': [],
    });
    open('/');
    const user = userEvent.setup();

    await screen.findByText("Let's build, athlete.");
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Sign-out failed, so you are still signed in. The server could not complete this. Please try again.');
    expect(screen.getByText("Let's build, athlete.")).toBeInTheDocument();
    await user.click(within(screen.getByRole('complementary', { name: 'Main navigation' })).getByRole('link', { name: /Goals/ }));
    expect(await screen.findByRole('heading', { level: 1, name: 'One goal at a time' })).toBeInTheDocument();
    expect(screen.queryByText(/Sign-out failed/)).not.toBeInTheDocument();
    online = true;
    await user.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(await screen.findByText('You have logged out.')).toBeInTheDocument();
    expect(screen.queryByText(/Sign-out failed/)).not.toBeInTheDocument();
  });

  it('treats a session that already ended as signed out', async () => {
    mockApi({ 'GET /api/session': athlete, 'GET /api/dashboard': overview, 'POST /api/session/logout': () => respond(401) });
    open('/');
    const user = userEvent.setup();

    await screen.findByText("Let's build, athlete.");
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(screen.queryByText(/Sign-out failed/)).not.toBeInTheDocument();
  });

  it('saves the device language on an account that has none and shows unknown pages', async () => {
    const calls = mockApi({ 'GET /api/session': { ...athlete, language: null }, 'PUT /api/profile/language': null });
    open('/nowhere');

    expect(await screen.findByText('This page does not exist.')).toBeInTheDocument();
    await vi.waitFor(() => expect(calls.find((c) => c.url === '/api/profile/language')?.body).toEqual({ language: 'en' }));
  });
});
