import type { CatalogExercise } from '../api/types/exercises';

export interface ExerciseChoice {
  groupId: string;
  subGroupId: string;
  exerciseId: string;
}

export function choiceOf(exercise: CatalogExercise | undefined): ExerciseChoice {
  return exercise
    ? { groupId: String(exercise.muscleGroupId), subGroupId: '', exerciseId: String(exercise.exerciseId) }
    : { groupId: '', subGroupId: '', exerciseId: '' };
}

export function chosenExercise(catalog: CatalogExercise[], choice: ExerciseChoice): CatalogExercise | undefined {
  return catalog.find((e) => e.exerciseId === Number(choice.exerciseId));
}
