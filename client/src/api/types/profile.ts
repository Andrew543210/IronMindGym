import type { EarnedAward, NamedOption } from './common';

export interface Profile {
  username: string;
  email: string;
  age: number;
  avatarName: string;
  strengthCoefficient: number;
  weightKg: number | null;
  heightCm: number | null;
  weighedOn: string | null;
  benchPressKg: number | null;
  squatKg: number | null;
  deadliftKg: number | null;
  weightHistory: { date: string; valueKg: number }[];
  achievements: EarnedAward[];
}

export type EnergyEquation = 'None' | 'Male' | 'Female';

export interface ProfileSettings {
  username: string;
  age: number;
  timeZone: string | null;
  weightKg: number;
  heightCm: number;
  benchPressKg: number;
  squatKg: number;
  deadliftKg: number;
  workoutsPerWeek: number;
  splitTypeId: number;
  energyEquation: EnergyEquation;
  calorieTarget: number | null;
  proteinTarget: number | null;
  fatTarget: number | null;
  carbsTarget: number | null;
}

export interface SettingsPage {
  settings: ProfileSettings;
  splitTypes: NamedOption[];
}

export interface SetupForm {
  splitTypes: NamedOption[];
  alreadyCompleted: boolean;
}
