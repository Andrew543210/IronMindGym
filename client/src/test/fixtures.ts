export const overview = {
  username: 'athlete',
  needsSetup: false,
  weightKg: 80,
  totalWorkouts: 3,
  todayNutrition: { caloriesKcal: 2100, proteinG: 150, fatG: 60, carbsG: 220 },
  goals: [{ id: 1, title: 'Squat 120 kg', deadline: '2026-12-20', isCompleted: false }],
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
    id: 7, publishedAt: '2021-05-01', title: 'Rest periods', thesisText: 'Longer rest helps strength.', source: 'Journal of Strength',
    sourceUrl: 'https://example.org/rest',
  }],
};

export const workout = {
  id: 4,
  name: 'Legs day',
  sessionDate: '2026-11-19T17:00:00Z',
  durationMinutes: 55,
  rating: 4,
  notes: 'Felt strong',
  exercises: [
    {
      exerciseName: 'Barbell Back Squat', exerciseType: 'StrengthTraining',
      sets: [
        { setNumber: 1, setTypeName: 'Warm-up', weightKg: 60, reps: 8, durationSeconds: 0, intensity: 0, restSeconds: 90 },
        { setNumber: 2, setTypeName: 'Working', weightKg: 100, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 150 },
      ],
    },
    { exerciseName: 'Rowing Machine', exerciseType: 'Cardio', sets: [{ setNumber: 1, setTypeName: null, weightKg: 0, reps: 0, durationSeconds: 600, intensity: 6, restSeconds: 0 }] },
    { exerciseName: 'Weighted Plank', exerciseType: 'Timed', sets: [{ setNumber: 1, setTypeName: null, weightKg: 10, reps: 0, durationSeconds: 60, intensity: 0, restSeconds: 60 }] },
  ],
  averageRestSeconds: 75,
  remainingWorkoutsThisWeek: 1,
  exerciseStats: [
    { exerciseName: 'Barbell Back Squat', setsCount: 2, tracksLoad: true, heaviestLoadKg: 100, percentOfPreviousBest: 105.3, isNewRecord: true },
    { exerciseName: 'Push-Ups', setsCount: 1, tracksLoad: true, heaviestLoadKg: 0, percentOfPreviousBest: null, isNewRecord: false },
    { exerciseName: 'Rowing Machine', setsCount: 1, tracksLoad: false, heaviestLoadKg: 0, percentOfPreviousBest: null, isNewRecord: false },
  ],
  isOwner: true,
  likesCount: 1,
  isLikedByCurrentUser: false,
  comments: [{ authorUsername: 'partner', text: 'Great depth!', date: '2026-11-19T18:00:00Z' }],
};

export function progressEntry(overrides: object = {}) {
  return {
    exerciseId: 7, exerciseName: 'Flat Bench Press', muscleGroup: 'Chest', muscleSubGroup: 'Middle Chest', exerciseType: 'StrengthTraining',
    hasCurrentResult: true, initialWeightKg: 60, initialReps: 8, initialDurationSeconds: 0, initialIntensity: 0,
    currentWeightKg: 70, currentReps: 6, currentDurationSeconds: 0, currentIntensity: 0,
    weightProgressKg: 10, repsProgressCount: -2, cardioDurationProgressSeconds: 0, cardioIntensityProgressCount: 0,
    ...overrides,
  };
}
