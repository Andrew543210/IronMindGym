
export interface NamedOption {
  id: number;
  name: string;
}

export type ExerciseType = 'StrengthTraining' | 'Cardio' | 'Timed';

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
