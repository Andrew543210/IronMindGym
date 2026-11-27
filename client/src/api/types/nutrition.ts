import type { DatedIntake, Intake } from './common';

export type TargetSource = 'NotSet' | 'Personal' | 'Estimate' | 'Remainder';

export interface Target {
  value: number;
  source: TargetSource;
}

export interface NutritionAdvice {
  calories: Target;
  protein: Target;
  fat: Target;
  carbs: Target;
  energyEquation: 'Male' | 'Female' | null;
  basalMetabolicRate: number;
  activityMultiplier: number;
  targetsSet: number;
  targetsMatched: number;
  score: number;
  targetsExceedEnergy: boolean;
  macroEnergyKcal: number;
  macrosDisagreeWithEnergy: boolean;
  weekDaysLogged: number;
  weekAverageKcal: number;
}

export interface NutritionDay extends Intake {
  date: string;
  isLogged: boolean;
  advice: NutritionAdvice;
  recentLogs: DatedIntake[];
}
