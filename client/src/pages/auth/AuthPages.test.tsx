import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { athlete, mockApi, renderPage, respond, sessionContext } from '../../test/harness';
import { LoginPage, RegisterPage } from './AuthPages';

describe('login', () => {
  it('shows why the last session ended and the server’s answer to a wrong password', async () => {
    const calls = mockApi({
      'POST /api/session/login': () => new Response(JSON.stringify({ title: 'Invalid email or password.' }),
        { status: 400, headers: { 'Content-Type': 'application/problem+json' } }),
    });
    renderPage('/login', <LoginPage />, undefined, sessionContext(null, { endedBy: 'passwordChanged' }));
    const user = userEvent.setup();

    expect(screen.getByRole('status')).toHaveTextContent('Password changed. Sign in with your new password.');
    await user.type(screen.getByLabelText('Email'), 'athlete@example.com');
    await user.type(screen.getByLabelText('Password'), 'wrong-password');
    await user.click(screen.getByLabelText('Remember me'));
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect(calls[0].body).toEqual({ email: 'athlete@example.com', password: 'wrong-password', rememberMe: true });
  });

  it('sends a signed-in athlete who has not finished the setup to it', () => {
    renderPage('/login', <LoginPage />, undefined, sessionContext({ ...athlete, setupComplete: false }));

    expect(screen.getByTestId('location')).toHaveTextContent('/setup');
  });

  it('warns when too many attempts were made', async () => {
    mockApi({ 'POST /api/session/login': () => respond(429) });
    renderPage('/login', <LoginPage />, undefined, sessionContext(null));
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'athlete@example.com');
    await user.type(screen.getByLabelText('Password'), 'TrainingPassword123!');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Too many attempts. Wait a minute and try again.');
  });
});

describe('registration', () => {
  async function fill(confirm: string) {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Username'), 'new_athlete');
    await user.type(screen.getByLabelText('Email'), 'new@example.com');
    await user.type(screen.getByLabelText('Age'), '30');
    await user.type(screen.getByLabelText('Password'), 'TrainingPassword123!');
    await user.type(screen.getByLabelText('Confirm password'), confirm);
    await user.click(screen.getByRole('button', { name: 'Create account' }));
  }

  it('sends an athlete who is already signed in to the overview or to the setup', () => {
    renderPage('/register', <RegisterPage />, undefined, sessionContext(athlete));
    expect(screen.getByTestId('location')).toHaveTextContent('/');
  });

  it('sends a signed-in athlete without a finished setup from registration to the setup', () => {
    renderPage('/register', <RegisterPage />, undefined, sessionContext({ ...athlete, setupComplete: false }));
    expect(screen.getByTestId('location')).toHaveTextContent('/setup');
  });

  it('checks the confirmation before creating the account', async () => {
    const calls = mockApi({});
    renderPage('/register', <RegisterPage />, undefined, sessionContext(null));

    await fill('SomethingElse123!');

    expect(screen.getByRole('alert')).toHaveTextContent('Passwords must match.');
    expect(calls).toHaveLength(0);
  });

  it('creates the account with the device time zone and signs in', async () => {
    vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue({ timeZone: 'Europe/Kyiv' } as Intl.ResolvedDateTimeFormatOptions);
    const signedIn = vi.fn();
    const calls = mockApi({ 'POST /api/session/register': { ...athlete, setupComplete: false } });
    renderPage('/register', <RegisterPage />, undefined, sessionContext(null, { signedIn }));

    await fill('TrainingPassword123!');

    await vi.waitFor(() => expect(signedIn).toHaveBeenCalledWith({ ...athlete, setupComplete: false }));
    expect(calls[0].body).toEqual({
      username: 'new_athlete', email: 'new@example.com', age: 30, password: 'TrainingPassword123!', confirmPassword: 'TrainingPassword123!', timeZone: 'Europe/Kyiv',
    });
  });
});
