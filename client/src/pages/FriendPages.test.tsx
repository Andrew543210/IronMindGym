import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Layout } from '../components/Layout';
import { mockApi, renderPage, renderRoutes } from '../test/harness';
import { workout } from '../test/fixtures';
import { FriendWorkoutPage, FriendsPage } from './FriendsPage';

const friend = {
  userId: 9, username: 'partner', isFriend: true, requestSent: false, requestReceived: false, averageKcal: 2250.5,
  lastFiveWorkouts: [{ workoutSessionId: 4, name: 'Legs day', sessionDate: '2026-11-19T17:00:00Z', durationMinutes: 55 }],
  lastNutritionLogs: [{ date: '2026-11-19T00:00:00', caloriesKcal: 2300, proteinG: 150, fatG: 70, carbsG: 240 }],
  personalRecords: [{ exerciseName: 'Barbell Back Squat', maxWeightKg: 120, updatedAt: '2026-11-19T17:00:00Z' }],
  achievements: [{ name: 'First Rep', conditionDescription: 'Complete your first workout', achievedAt: '2026-11-01T10:00:00Z' }],
};

function friendsPage() {
  return renderRoutes([{ element: <Layout />, children: [{ path: '/friends', element: <FriendsPage /> }] }], '/friends');
}

describe('friends', () => {
  it('shows what a friend shares and removes the connection after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    let friends = [friend];
    mockApi({
      'GET /api/friends?search=': () => ({ friends }),
      'DELETE /api/friends/9': () => {
        friends = [];
        return null;
      },
    });
    friendsPage();
    const user = userEvent.setup();

    expect(await screen.findByText('Status: Friend')).toBeInTheDocument();
    expect(screen.getByText('2250.5')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Details' })).toHaveAttribute('href', '/friends/9/workouts/4');
    expect(screen.getAllByText('2026-11-19')).toHaveLength(2);
    expect(screen.getByText('First Rep')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'VIEW PROGRESS TRACKER' })).toHaveAttribute('href', '/friends/9/progress');

    await user.click(screen.getByRole('button', { name: 'Remove friend' }));

    expect(window.confirm).toHaveBeenCalledWith('Remove partner from your friends? They will no longer see your training.');
    expect(await screen.findByText('Connection removed.')).toBeInTheDocument();
    expect(await screen.findByText('No connections yet. Search for a username to invite a training partner.')).toBeInTheDocument();
  });

  it('accepts an incoming request and declines nothing it was not asked to', async () => {
    let accepted = false;
    const calls = mockApi({
      'GET /api/friends?search=': () => ({
        friends: [{ ...friend, isFriend: accepted, requestReceived: !accepted, lastFiveWorkouts: [], lastNutritionLogs: [], personalRecords: [], achievements: [] }],
      }),
      'POST /api/friends/9/accept': () => {
        accepted = true;
        return null;
      },
    });
    friendsPage();
    const user = userEvent.setup();

    expect(await screen.findByText('Status: Wants to connect')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Decline' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Accept request' }));

    expect(await screen.findByText("You're now connected. Your friend can see your workouts, nutrition, records and progress.")).toBeInTheDocument();
    expect(await screen.findByText('No workouts yet.')).toBeInTheDocument();
    expect(screen.getByText('No nutrition logs yet.')).toBeInTheDocument();
    expect(screen.getByText('No personal records yet.')).toBeInTheDocument();
    expect(screen.getByText('No awards yet.')).toBeInTheDocument();
    expect(calls.filter((c) => c.method !== 'GET')).toHaveLength(1);
  });

  it('says when a search finds nobody', async () => {
    mockApi({ 'GET /api/friends?search=': { friends: [] }, 'GET /api/friends?search=ghost': { friends: [] } });
    friendsPage();
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Username'), 'ghost');
    await user.click(screen.getByRole('button', { name: 'Search' }));

    expect(await screen.findByText('No athletes match “ghost”.')).toBeInTheDocument();
  });
});

describe('a friend’s workout', () => {
  it('likes and comments on it', async () => {
    let liked = false;
    const route = '/api/friends/9/workouts/4';
    const calls = mockApi({
      [`GET ${route}`]: () => ({ friendUsername: 'partner', workout: { ...workout, isOwner: false, isLikedByCurrentUser: liked, likesCount: liked ? 2 : 1 } }),
      [`PUT ${route}/like`]: () => {
        liked = true;
        return null;
      },
      [`POST ${route}/comments`]: null,
    });
    renderPage('/friends/:friendId/workouts/:sessionId', <FriendWorkoutPage />, '/friends/9/workouts/4');
    const user = userEvent.setup();

    expect(await screen.findByRole('heading', { name: 'partner - Workout Details' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Like' }));
    expect(await screen.findByRole('button', { name: 'Unlike' })).toHaveAttribute('aria-pressed', 'true');
    await user.type(screen.getByLabelText('Comment'), 'Strong!');
    await user.click(screen.getByRole('button', { name: 'Post' }));

    await vi.waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ text: 'Strong!' }));
    expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({ liked: true });
  });
});

