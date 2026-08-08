import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { setLanguage } from '../i18n/language';
import { mockApi, renderPage } from '../test/harness';
import { AwardsPage, RecordsPage, StatisticsPage } from './StatisticsPages';

const record = { muscleGroupName: 'Chest', muscleSubGroupName: 'Middle Chest', exerciseName: 'Flat Bench Press', maxWeightKg: 82.5, updatedAt: '2026-11-19T21:30:00Z' };

const statistics = {
  workoutsCompleted: 12, totalWorkoutMinutes: 600, avgWorkoutDurationMinutes: 50, longestStreak: 3, avgWeeklyWorkouts: 2.4,
  totalSetsCount: 180, avgSetsPerWorkout: 15, avgRestSeconds: 95, totalVolume: 52000, monthlyTotalTonnage: 21000, weeklyProgress: 4800,
  last7DaysWorkouts: 2, last7DaysMinutes: 95, uniqueExercisesCount: 14, exercisesThisMonth: 9, favoriteExerciseName: 'Flat Bench Press',
  favoriteExerciseCount: 1, bestProgressPercent: 12.5, bestProgressExercise: 'Barbell Back Squat', totalAwardsCount: 4, recentRecords: [record],
};

describe('statistics', () => {
  it('shows the totals, the last week and recent records in the athlete’s calendar', async () => {
    mockApi({ 'GET /api/statistics': statistics });
    renderPage('/statistics', <StatisticsPage />);

    expect(await screen.findByText('52000')).toBeInTheDocument();
    expect(screen.getByText('3 days')).toBeInTheDocument();
    expect(screen.getByText('2 workouts · 95 min')).toBeInTheDocument();
    expect(screen.getByText('In 1 workout')).toBeInTheDocument();
    expect(screen.getByText('Barbell Back Squat')).toBeInTheDocument();
    expect(screen.getByText('82.5 kg')).toBeInTheDocument();
    expect(screen.getByText('2026-11-19')).toBeInTheDocument();
  });

  it('explains empty statistics in Ukrainian', async () => {
    setLanguage('uk');
    mockApi({
      'GET /api/statistics': {
        ...statistics, workoutsCompleted: 0, longestStreak: 5, favoriteExerciseName: null, bestProgressExercise: null, bestProgressPercent: 0, recentRecords: [],
      },
    });
    renderPage('/statistics', <StatisticsPage />);

    expect(await screen.findByText('5 днів')).toBeInTheDocument();
    expect(screen.getByText('Запишіть тренування, щоб побачити її тут.')).toBeInTheDocument();
    expect(screen.getByText('Рекордів поки немає. Запишіть силовий підхід із додатковою вагою, щоб встановити перший.')).toBeInTheDocument();
  });

  it('lists every record with its muscle group', async () => {
    mockApi({ 'GET /api/statistics/records': [record] });
    renderPage('/statistics/records', <RecordsPage />);

    expect(await screen.findByText('Middle Chest')).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Focus area' })).toBeInTheDocument();
  });
});

describe('awards', () => {
  it('shows unlocked awards and progress towards the rest', async () => {
    mockApi({
      'GET /api/awards': [
        { code: 'PROFILE_SETUP', name: 'Fully Set', conditionDescription: 'Complete your setup', isUnlocked: true, isNumeric: false, currentValue: 0, targetValue: 0, progressPercent: 100 },
        { code: 'WORKOUT_010', name: 'Ten Strong', conditionDescription: 'Complete 10 workouts', isUnlocked: false, isNumeric: true, currentValue: 4, targetValue: 10, progressPercent: 40 },
      ],
    });
    renderPage('/awards', <AwardsPage />);

    expect(await screen.findByText('1 of 2 unlocked')).toBeInTheDocument();
    expect(screen.getByText('Current: 4 / Target: 10 (40%)')).toBeInTheDocument();
    expect(screen.getByRole('progressbar', { name: 'Ten Strong' })).toHaveAttribute('aria-valuenow', '40');
    expect(screen.getByText('Unlocks once the related step is done.')).toBeInTheDocument();
  });
});
