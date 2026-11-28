import type { ExerciseType } from './common';

export interface ProgressEntry {
  exerciseId: number;
  exerciseName: string;
  muscleGroup: string;
  muscleSubGroup: string;
  exerciseType: ExerciseType;
  hasCurrentResult: boolean;
  initialWeightKg: number;
  initialReps: number;
  initialDurationSeconds: number;
  initialIntensity: number;
  currentWeightKg: number;
  currentReps: number;
  currentDurationSeconds: number;
  currentIntensity: number;
  weightProgressKg: number;
  repsProgressCount: number;
  cardioDurationProgressSeconds: number;
  cardioIntensityProgressCount: number;
}

export interface ResultPair {
  initialValue1: number;
  initialValue2: number;
  currentValue1: number;
  currentValue2: number;
}

export interface FriendProgress {
  friendUsername: string;
  currentStrengthCoefficient: number;
  exercises: ProgressEntry[];
}
