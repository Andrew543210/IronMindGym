import type { DatedIntake, EarnedAward, WorkoutListItem } from './common';
import type { WorkoutDetails } from './workouts';

export interface FriendCard {
  userId: number;
  username: string;
  isFriend: boolean;
  requestSent: boolean;
  requestReceived: boolean;
  averageKcal: number;
  lastFiveWorkouts: WorkoutListItem[];
  lastNutritionLogs: DatedIntake[];
  personalRecords: { exerciseName: string; maxWeightKg: number; achievedAt: string }[];
  achievements: EarnedAward[];
}

export interface FriendIndex {
  friends: FriendCard[];
}

export type FriendRequestResult = 'Sent' | 'AlreadyRequested' | 'Connected' | 'AlreadyConnected';

export interface FriendWorkout {
  friendUsername: string;
  workout: WorkoutDetails;
}
