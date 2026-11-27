import type { CatalogExercise } from '../api/types/exercises';

/** The group, the optional focus area and the exercise chosen, as ids: names change with the interface language. Empty means none. */
export interface ExerciseChoice {
  groupId: string;
  subGroupId: string;
  exerciseId: string;
}

/** The choice that shows an exercise in its group, or nothing chosen. */
export function choiceOf(exercise: CatalogExercise | undefined): ExerciseChoice {
  return exercise
    ? { groupId: String(exercise.muscleGroupId), subGroupId: '', exerciseId: String(exercise.exerciseId) }
    : { groupId: '', subGroupId: '', exerciseId: '' };
}

export function chosenExercise(catalog: CatalogExercise[], choice: ExerciseChoice): CatalogExercise | undefined {
  return catalog.find((e) => e.exerciseId === Number(choice.exerciseId));
}
