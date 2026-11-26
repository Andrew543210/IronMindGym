import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { mockApi, renderPage } from '../test/harness';
import { GoalsPage } from './GoalsPage';

it('adds a goal, completes it and deletes another', async () => {
  vi.spyOn(window, 'confirm').mockReturnValue(true);
  let goals = [
    { id: 1, title: 'Squat 120 kg', deadline: '2026-12-20', isCompleted: false },
    { id: 2, title: 'Run 5 km', deadline: '2026-11-01', isCompleted: false },
  ];
  const calls = mockApi({
    'GET /api/goals': () => goals,
    'POST /api/goals': () => {
      goals = [...goals, { id: 3, title: 'Bench 100 kg', deadline: '2026-12-20', isCompleted: false }];
      return null;
    },
    'PUT /api/goals/1/completed': () => {
      goals = goals.map((g) => (g.id === 1 ? { ...g, isCompleted: true } : g));
      return null;
    },
    'DELETE /api/goals/2': () => {
      goals = goals.filter((g) => g.id !== 2);
      return null;
    },
  });
  renderPage('/goals', <GoalsPage />);
  const user = userEvent.setup();

  expect(await screen.findByText('0 / 2 complete')).toBeInTheDocument();
  expect(screen.getByText(/Past target date/)).toBeInTheDocument();
  expect(screen.getByLabelText('Target date')).toHaveValue('2026-12-20');

  await user.type(screen.getByLabelText('What are you working toward?'), 'Bench 100 kg');
  await user.click(screen.getByRole('button', { name: 'Add goal ＋' }));
  expect(await screen.findByText('Bench 100 kg')).toBeInTheDocument();
  expect(screen.getByLabelText('What are you working toward?')).toHaveValue('');

  const squat = screen.getByText('Squat 120 kg').closest('.goal-row') as HTMLElement;
  await user.click(within(squat).getByRole('button', { name: 'Complete ✓' }));
  expect(await screen.findByText('1 / 3 complete')).toBeInTheDocument();
  expect(within(squat).getByRole('button', { name: 'Reopen' })).toBeInTheDocument();

  await user.click(screen.getByRole('button', { name: 'Delete goal Run 5 km' }));
  expect(window.confirm).toHaveBeenCalledWith('Delete the goal "Run 5 km"? This cannot be undone.');
  expect(await screen.findByText('1 / 2 complete')).toBeInTheDocument();
  expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ title: 'Bench 100 kg', deadline: '2026-12-20' });
  expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({ completed: true });
});

it('keeps the goals when the athlete cancels a delete and invites a first goal', async () => {
  vi.spyOn(window, 'confirm').mockReturnValue(false);
  const calls = mockApi({ 'GET /api/goals': [] });
  renderPage('/goals', <GoalsPage />);

  expect(await screen.findByText('Make your next step clear.')).toBeInTheDocument();
  expect(calls).toHaveLength(1);
});
