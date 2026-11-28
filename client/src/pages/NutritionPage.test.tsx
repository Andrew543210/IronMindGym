import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { mockApi, renderPage } from '../test/harness';
import { NutritionPage } from './NutritionPage';

const notSet = { value: 0, source: 'NotSet' };
const advice = {
  calories: { value: 2715, source: 'Estimate' }, protein: { value: 150, source: 'Personal' }, fat: { value: 64, source: 'Estimate' },
  carbs: { value: 385, source: 'Remainder' }, energyEquation: 'Male', basalMetabolicRate: 1810, activityMultiplier: 1.5,
  targetsSet: 4, targetsMatched: 2, score: 50, targetsExceedEnergy: false, macroEnergyKcal: 2300, macrosDisagreeWithEnergy: true,
  weekDaysLogged: 2, weekAverageKcal: 2250,
};
const entry = { caloriesKcal: 2500, proteinG: 140, fatG: 70, carbsG: 250 };

function day(date: string, logged: boolean, overrides: object = {}) {
  return {
    date, isLogged: logged, ...(logged ? entry : { caloriesKcal: 0, proteinG: 0, fatG: 0, carbsG: 0 }), advice,
    recentLogs: [{ date: '2026-11-20', ...entry }, { date: '2026-11-18', ...entry, caloriesKcal: 2000 }],
    ...overrides,
  };
}

describe('nutrition journal', () => {
  it('compares a logged day with the targets and explains each target', async () => {
    mockApi({ 'GET /api/nutrition?date=2026-11-20': day('2026-11-20', true) });
    renderPage('/nutrition', <NutritionPage />);

    expect(await screen.findByText('2 of 4 targets within 10%')).toBeInTheDocument();
    expect(screen.getByText('Target match: 50/100')).toBeInTheDocument();
    expect(screen.getByText('Mifflin–St Jeor (male): 10 × weight + 6.25 × height − 5 × age + 5.')).toBeInTheDocument();
    expect(screen.getByText('Your personal target.')).toBeInTheDocument();
    expect(screen.getByText('Remaining energy: (calories − protein × 4 − fat × 9) / 4.')).toBeInTheDocument();
    expect(screen.getByText('Energy: 2500 logged / 2715 target.')).toBeInTheDocument();
    expect(screen.getByText('Macros add up to about 2300 kcal. Check serving sizes if this differs from your energy total.')).toBeInTheDocument();
    expect(screen.getByText('Average across 2 logged days this week: 2250 kcal.')).toBeInTheDocument();
    expect(screen.getByLabelText('Calories (kcal)')).toHaveValue(2500);
    expect(screen.getByText('Selected')).toBeInTheDocument();
  });

  it('opens a missed day, saves it and reloads the journal', async () => {
    let saved = false;
    const calls = mockApi({
      'GET /api/nutrition?date=2026-11-20': day('2026-11-20', true),
      'GET /api/nutrition?date=2026-11-18': () => day('2026-11-18', saved),
      'PUT /api/nutrition/days/2026-11-18': () => {
        saved = true;
        return null;
      },
    });
    renderPage('/nutrition', <NutritionPage />);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: 'Edit' }));
    expect(await screen.findByText('Nothing logged for this day yet.')).toBeInTheDocument();
    for (const [label, value] of [['Calories (kcal)', '1900'], ['Protein (g)', '120'], ['Fat (g)', '60'], ['Carbs (g)', '210.5']] as const)
      await user.type(screen.getByLabelText(label), value);
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Entry saved for Nov 18, 2026.')).toBeInTheDocument();
    expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({ caloriesKcal: 1900, proteinG: 120, fatG: 60, carbsG: 210.5 });
    expect(await screen.findByRole('button', { name: 'Delete entry' })).toBeInTheDocument();
  });

  it('hides the form of the previous day while another day loads', async () => {
    let release = () => {};
    const later = new Promise<object>((resolve) => {
      release = () => resolve(day('2026-11-18', true, { caloriesKcal: 1800 }));
    });
    const calls = mockApi({
      'GET /api/nutrition?date=2026-11-20': day('2026-11-20', true),
      'GET /api/nutrition?date=2026-11-18': () => later,
    });
    renderPage('/nutrition', <NutritionPage />);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: 'Edit' }));

    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete entry' })).not.toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Loading');
    release();
    expect(await screen.findByLabelText('Calories (kcal)')).toHaveValue(1800);
    expect(calls.filter((c) => c.method !== 'GET')).toEqual([]);
  });

  it('deletes an entry after confirmation and shows when estimates are off', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    let deleted = false;
    const off = { ...advice, energyEquation: null, basalMetabolicRate: 0, activityMultiplier: 0, calories: notSet, protein: notSet, fat: notSet, carbs: notSet, targetsSet: 0, score: 0, macrosDisagreeWithEnergy: false, weekDaysLogged: 0 };
    mockApi({
      'GET /api/nutrition?date=2026-11-20': () => day('2026-11-20', !deleted, { advice: off }),
      'DELETE /api/nutrition/days/2026-11-20': () => {
        deleted = true;
        return null;
      },
    });
    renderPage('/nutrition', <NutritionPage />);
    const user = userEvent.setup();

    expect(await screen.findByText('Targets not set')).toBeInTheDocument();
    expect(screen.getByText('Set your daily targets in Edit profile, or choose an adult energy equation to see an estimate.')).toBeInTheDocument();
    expect(screen.getAllByText('Automatic estimates are off. Set personal targets in your profile.')).toHaveLength(2);
    await user.click(screen.getByRole('button', { name: 'Delete entry' }));

    expect(await screen.findByText('Entry deleted.')).toBeInTheDocument();
    expect(window.confirm).toHaveBeenCalledWith('Delete the nutrition entry for Nov 20, 2026?');
    expect(await screen.findByText('Nothing logged for this day yet.')).toBeInTheDocument();
    expect(screen.getByLabelText('Calories (kcal)')).toHaveValue(null);
  });
});
