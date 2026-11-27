/** Shapes the API uses in several areas. Names, units and dates come as the server sends them. */

export interface NamedOption {
  id: number;
  name: string;
}

export type ExerciseType = 'StrengthTraining' | 'Cardio' | 'Timed';

/** The four totals of a day of the nutrition journal. */
export interface Intake {
  caloriesKcal: number;
  proteinG: number;
  fatG: number;
  carbsG: number;
}

export interface DatedIntake extends Intake {
  date: string;
}

export interface EarnedAward {
  name: string;
  conditionDescription: string;
  achievedAt: string;
}

export interface WorkoutListItem {
  id: number;
  name: string;
  sessionDate: string;
  durationMinutes: number;
}
