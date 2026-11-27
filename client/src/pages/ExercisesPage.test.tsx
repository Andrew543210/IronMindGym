import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it } from 'vitest';
import { currentLanguage, setLanguage } from '../i18n/language';
import { mockApi, renderPage } from '../test/harness';
import { ExercisesPage } from './ExercisesPage';

it('filters the library by group, subgroup and text', async () => {
  mockApi({
    'GET /api/exercises': {
      muscleGroups: [{ id: 1, name: 'Chest' }, { id: 2, name: 'Core' }],
      exercises: [
        { exerciseId: 1, exerciseName: 'Flat Bench Press', englishName: 'Flat Bench Press', description: 'Press the bar from the chest.', exerciseType: 'StrengthTraining', muscleGroupId: 1, muscleGroupName: 'Chest', muscleSubGroupId: 1, muscleSubGroupName: 'Middle Chest' },
        { exerciseId: 2, exerciseName: 'Incline Press', englishName: 'Incline Press', description: 'Press on an incline bench.', exerciseType: 'StrengthTraining', muscleGroupId: 1, muscleGroupName: 'Chest', muscleSubGroupId: 2, muscleSubGroupName: 'Upper Chest' },
        { exerciseId: 3, exerciseName: 'Weighted Plank', englishName: 'Weighted Plank', description: 'Hold a straight line.', exerciseType: 'Timed', muscleGroupId: 2, muscleGroupName: 'Core', muscleSubGroupId: 3, muscleSubGroupName: 'Abs' },
      ],
    },
  });
  renderPage('/exercises', <ExercisesPage />);
  const user = userEvent.setup();

  expect(await screen.findByText('3 exercises found')).toBeInTheDocument();
  expect(screen.getByText('Timed')).toBeInTheDocument();

  await user.selectOptions(screen.getByLabelText('Muscle group'), 'Chest');
  expect(screen.getByText('2 exercises found')).toBeInTheDocument();
  expect(screen.getAllByRole('option').map((o) => o.textContent)).toContain('Upper Chest');
  await user.selectOptions(screen.getByLabelText('Subgroup'), 'Upper Chest');
  expect(screen.getByText('1 exercise found')).toBeInTheDocument();
  expect(screen.getByText('Incline Press')).toBeInTheDocument();

  await user.selectOptions(screen.getByLabelText('Muscle group'), '');
  await user.type(screen.getByLabelText('Search'), 'STRAIGHT');
  expect(await screen.findByText('Weighted Plank')).toBeInTheDocument();
  await user.type(screen.getByLabelText('Search'), ' nothing');
  expect(await screen.findByText('No exercises match these filters.')).toBeInTheDocument();
});

it('keeps the chosen subgroup when the language changes', async () => {
  const exercise = { exerciseId: 1, englishName: 'Flat Bench Press', description: '', exerciseType: 'StrengthTraining', muscleGroupId: 1, muscleSubGroupId: 1 };
  const other = { ...exercise, exerciseId: 2, englishName: 'Incline Press', muscleSubGroupId: 2 };
  mockApi({
    'GET /api/exercises': () => currentLanguage() === 'uk'
      ? { muscleGroups: [{ id: 1, name: 'Груди' }], exercises: [
        { ...exercise, exerciseName: 'Жим лежачи', muscleGroupName: 'Груди', muscleSubGroupName: 'Середина грудей' },
        { ...other, exerciseName: 'Жим на похилій', muscleGroupName: 'Груди', muscleSubGroupName: 'Верх грудей' }] }
      : { muscleGroups: [{ id: 1, name: 'Chest' }], exercises: [
        { ...exercise, exerciseName: 'Flat Bench Press', muscleGroupName: 'Chest', muscleSubGroupName: 'Middle Chest' },
        { ...other, exerciseName: 'Incline Press', muscleGroupName: 'Chest', muscleSubGroupName: 'Upper Chest' }] },
  });
  renderPage('/exercises', <ExercisesPage />);
  const user = userEvent.setup();
  await user.selectOptions(await screen.findByLabelText('Subgroup'), 'Upper Chest');
  expect(screen.getByText('1 exercise found')).toBeInTheDocument();

  act(() => setLanguage('uk'));

  expect(await screen.findByText('Жим на похилій')).toBeInTheDocument();
  expect(screen.queryByText('Жим лежачи')).not.toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'Верх грудей', selected: true })).toBeInTheDocument();
});
