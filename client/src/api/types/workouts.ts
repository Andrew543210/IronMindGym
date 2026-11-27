import type { ExerciseType, NamedOption, WorkoutListItem } from './common';
import type { CatalogExercise } from './exercises';

export interface WorkoutHistory {
  sessions: WorkoutListItem[];
  page: number;
  hasOlderSessions: boolean;
}

export interface WorkoutSet {
  setNumber: number;
  setTypeName: string;
  weightKg: number;
  reps: number;
  durationSeconds: number;
  intensity: number;
  restSeconds: number;
}

export interface ExerciseStat {
  exerciseName: string;
  setsCount: number;
  tracksLoad: boolean;
  heaviestLoadKg: number;
  percentOfPreviousBest: number | null;
  isNewRecord: boolean;
}

export interface WorkoutComment {
  authorUsername: string;
  text: string;
  createdAt: string;
}

export interface WorkoutDetails {
  id: number;
  name: string;
  sessionDate: string;
  durationMinutes: number;
  rating: number | null;
  notes: string | null;
  exercises: { exerciseName: string; exerciseType: ExerciseType; sets: WorkoutSet[] }[];
  averageRestSeconds: number;
  remainingWorkoutsThisWeek: number | null;
  exerciseStats: ExerciseStat[];
  isOwner: boolean;
  likesCount: number;
  isLikedByCurrentUser: boolean;
  comments: WorkoutComment[];
}

export interface LoggerOptions {
  activeSplitTypeName: string | null;
  activeSplitTypeId: number | null;
  splitTypes: (NamedOption & { days: NamedOption[] })[];
  setTypes: NamedOption[];
  defaultSetTypeId: number;
  exercises: CatalogExercise[];
}

export interface PreviousSet {
  weightKg: number;
  reps: number;
  durationSeconds: number;
  intensity: number;
  restSeconds: number;
  setTypeId: number;
  localDate: string;
}
