import type { ExerciseType, NamedOption } from './common';

export interface CatalogExercise {
  exerciseId: number;
  exerciseName: string;
  englishName: string;
  description: string;
  exerciseType: ExerciseType;
  muscleGroupId: number;
  muscleGroupName: string;
  muscleSubGroupId: number;
  muscleSubGroupName: string;
}

export interface ExerciseLibrary {
  muscleGroups: NamedOption[];
  exercises: CatalogExercise[];
}
