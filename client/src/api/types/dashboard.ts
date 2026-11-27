import type { Intake, WorkoutListItem } from './common';
import type { Goal } from './goals';

export interface ResearchNote {
  id: number;
  publishedAt: string;
  title: string;
  thesisText: string;
  source: string;
  sourceUrl: string;
}

export interface RankStep {
  name: string;
  minStrengthCoefficient: number;
  isCurrent: boolean;
}

export interface Overview {
  username: string;
  needsSetup: boolean;
  weightKg: number | null;
  totalWorkouts: number;
  todayNutrition: Intake | null;
  goals: Goal[];
  recentWorkouts: WorkoutListItem[];
  avatarName: string;
  strengthCoefficient: number;
  currentAvatarPositionPercent: number;
  avatarScalePoints: RankStep[];
  plannedWorkoutsPerWeek: number;
  completedThisWeek: number;
  feedItems: ResearchNote[];
}
