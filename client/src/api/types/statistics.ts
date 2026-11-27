export interface PersonalRecord {
  muscleGroupName: string;
  muscleSubGroupName: string;
  exerciseName: string;
  maxWeightKg: number;
  achievedAt: string;
}

export interface Statistics {
  workoutsCompleted: number;
  totalWorkoutMinutes: number;
  avgWorkoutDurationMinutes: number;
  longestStreak: number;
  avgWeeklyWorkouts: number;
  totalSetsCount: number;
  avgSetsPerWorkout: number;
  avgRestSeconds: number;
  totalVolume: number;
  monthlyTotalTonnage: number;
  weeklyProgress: number;
  last7DaysWorkouts: number;
  last7DaysMinutes: number;
  uniqueExercisesCount: number;
  exercisesThisMonth: number;
  favoriteExerciseName: string | null;
  favoriteExerciseCount: number;
  bestProgressPercent: number;
  bestProgressExercise: string | null;
  totalAwardsCount: number;
  recentRecords: PersonalRecord[];
}

export interface AwardProgress {
  code: string;
  name: string;
  conditionDescription: string;
  isUnlocked: boolean;
  isNumeric: boolean;
  currentValue: number;
  targetValue: number;
  progressPercent: number;
}
