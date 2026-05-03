export const overview = {
  username: 'athlete',
  needsSetup: false,
  weightKg: 80,
  totalWorkouts: 3,
  todayNutrition: { caloriesKcal: 2100, proteinG: 150, fatG: 60, carbsG: 220 },
  goals: [{ id: 1, title: 'Squat 120 kg', deadline: '2026-12-20T00:00:00', isCompleted: false }],
  recentWorkouts: [{ id: 4, name: 'Legs day', sessionDate: '2026-11-19T17:00:00Z', durationMinutes: 55 }],
  avatarName: 'Gym Centurion',
  strengthCoefficient: 3,
  currentAvatarPositionPercent: 60,
  avatarScalePoints: [
    { name: 'Noodle Nomad', minStrengthCoefficient: 0, isCurrent: false },
    { name: 'Gym Centurion', minStrengthCoefficient: 3, isCurrent: true },
  ],
  plannedWorkoutsPerWeek: 3,
  completedThisWeek: 2,
  feedItems: [{
    feedId: 7, publishedAt: '2021-05-01T00:00:00Z', title: 'Rest periods', thesisText: 'Longer rest helps strength.', source: 'Journal of Strength',
    sourceUrl: 'https://example.org/rest',
  }],
};

