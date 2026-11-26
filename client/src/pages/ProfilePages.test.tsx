import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { mockApi, renderPage, sessionContext } from '../test/harness';
import { ChangePasswordPage, ProfilePage, ProfileSettingsPage } from './ProfilePages';

const profile = {
  username: 'athlete', email: 'athlete@example.com', age: 24, avatarName: 'Gym Centurion', strengthCoefficient: 3,
  weightKg: 80, heightCm: 180, weighedOn: '2026-11-19', benchPressKg: 60, squatKg: 80, deadliftKg: 100,
  weightHistory: [{ date: '2026-11-19', valueKg: 80.5 }, { date: '2026-11-18', valueKg: 81 }],
  achievements: [{ name: 'Fully Set', conditionDescription: 'Complete profile, anthropometry and setup', achievedAt: '2026-11-01T08:00:00Z' }],
};

const settings = {
  username: 'athlete', age: 24, timeZone: 'Europe/Kyiv', weightKg: 80, heightCm: 180, benchPressKg: 60, squatKg: 80, deadliftKg: 100,
  workoutsPerWeek: 3, splitTypeId: 2, energyEquation: 'None', calorieTarget: 2400, proteinTarget: null, fatTarget: null, carbsTarget: null,
};

describe('profile', () => {
  it('shows the account, body and awards and logs a weigh-in', async () => {
    const calls = mockApi({ 'GET /api/profile': profile, 'PUT /api/profile/weights/2026-11-20': null });
    renderPage('/profile', <ProfilePage />);
    const user = userEvent.setup();

    expect(await screen.findByText('Gym Centurion')).toBeInTheDocument();
    expect(screen.getByText('180.0 cm')).toBeInTheDocument();
    expect(screen.getByText('Fully Set')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Weight over your last 2 entries' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Export my data ↓' })).toHaveAttribute('href', '/api/profile/export');

    await user.type(screen.getByLabelText('Weight (kg)'), '79.8');
    await user.click(screen.getByRole('button', { name: 'Save weight' }));

    expect(await screen.findByText('Weight saved for Nov 20, 2026.')).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({ weightKg: 79.8 });
    expect(calls.filter((c) => c.url === '/api/profile')).toHaveLength(2);
  });

  it('invites a first weigh-in when the history is empty', async () => {
    mockApi({ 'GET /api/profile': { ...profile, weightKg: null, heightCm: null, weighedOn: null, weightHistory: [], achievements: [] } });
    renderPage('/profile', <ProfilePage />);

    expect(await screen.findByText('Your first entry starts the story.')).toBeInTheDocument();
    expect(screen.getByText('No awards yet. Your first workout earns one.')).toBeInTheDocument();
  });
});

describe('profile settings', () => {
  it('saves the edited form with empty targets as null and returns to the profile', async () => {
    const refresh = vi.fn(async () => {});
    const calls = mockApi({
      'GET /api/profile/settings': { settings, splitTypes: [{ id: 1, name: 'Full-body' }, { id: 2, name: 'Upper-Lower' }] },
      'PUT /api/profile/settings': null,
    });
    renderPage('/profile/settings', <ProfileSettingsPage />, undefined, sessionContext(undefined, { refresh }));
    const user = userEvent.setup();

    const age = await screen.findByLabelText('Age');
    await user.clear(age);
    await user.type(age, '25');
    await user.clear(screen.getByLabelText('Calories (kcal)'));
    await user.type(screen.getByLabelText('Protein (g)'), '150');
    await user.selectOptions(screen.getByLabelText('Training split'), 'Full-body');
    await user.selectOptions(screen.getByLabelText('Automatic energy estimate'), 'Male');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(await screen.findByTestId('location')).toHaveTextContent('/profile Profile updated.');
    expect(refresh).toHaveBeenCalled();
    expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({
      ...settings, age: 25, splitTypeId: 1, energyEquation: 'Male', calorieTarget: null, proteinTarget: 150,
    });
  });

  it('fills the time zone from the device and shows server errors', async () => {
    vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue({ timeZone: 'America/New_York' } as Intl.ResolvedDateTimeFormatOptions);
    mockApi({
      'GET /api/profile/settings': { settings: { ...settings, timeZone: null }, splitTypes: [{ id: 2, name: 'Upper-Lower' }] },
      'PUT /api/profile/settings': () => new Response(JSON.stringify({ errors: { Username: ['This username is already taken.'] } }),
        { status: 400, headers: { 'Content-Type': 'application/problem+json' } }),
    });
    renderPage('/profile/settings', <ProfileSettingsPage />);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: 'Use this device' }));
    expect(screen.getByLabelText('Time zone')).toHaveValue('America/New_York');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This username is already taken.');
  });
});

describe('password', () => {
  it('checks the confirmation, then ends the session and asks to sign in again', async () => {
    const signedOut = vi.fn();
    const calls = mockApi({ 'PUT /api/profile/password': null });
    renderPage('/profile/password', <ChangePasswordPage />, undefined, sessionContext(undefined, { signedOut }));
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Current password'), 'TrainingPassword123!');
    await user.type(screen.getByLabelText('New password'), 'NewTrainingPassword123!');
    await user.type(screen.getByLabelText('Confirm password'), 'Different123!!');
    await user.click(screen.getByRole('button', { name: 'Update password' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Passwords must match.');
    expect(calls).toHaveLength(0);

    const confirm = screen.getByLabelText('Confirm password');
    await user.clear(confirm);
    await user.type(confirm, 'NewTrainingPassword123!');
    await user.click(screen.getByRole('button', { name: 'Update password' }));

    await vi.waitFor(() => expect(signedOut).toHaveBeenCalledWith('passwordChanged'));
    expect(calls).toEqual([expect.objectContaining({ method: 'PUT', body: {
      currentPassword: 'TrainingPassword123!', newPassword: 'NewTrainingPassword123!', confirmPassword: 'NewTrainingPassword123!',
    } })]);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
