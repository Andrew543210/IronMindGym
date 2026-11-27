using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class StatisticsService(IronMindDbContext db, AthleteCalendars calendars)
{
    public async Task<TrainingStatistics> BuildAsync(int userId, CancellationToken cancellationToken)
    {
        var calendar = await calendars.ForAsync(userId, cancellationToken);
        var now = calendar.UtcNow;
        var monthAgo = calendar.MonthAgoUtc;
        var weekAgo = now.AddDays(-7);
        var sessions = db.WorkoutSessions.Where(s => s.UserId == userId);
        var performed = db.PerformedExercises.Where(pe => pe.WorkoutSession.UserId == userId);
        var sets = db.Sets.Where(s => s.PerformedExercise.WorkoutSession.UserId == userId);

        var sessionTotals = await sessions.GroupBy(_ => 1).Select(g => new
        {
            Count = g.Count(),
            Minutes = g.Sum(s => s.DurationMinutes),
            First = g.Min(s => s.SessionDate),
            WeekCount = g.Count(s => s.SessionDate >= weekAgo),
            WeekMinutes = g.Where(s => s.SessionDate >= weekAgo).Sum(s => s.DurationMinutes)
        }).SingleOrDefaultAsync(cancellationToken);
        var setTotals = await sets.GroupBy(_ => 1).Select(g => new
        {
            Count = g.Count(),
            AverageRest = g.Average(s => (double)s.RestSeconds),
            // Volume is external load × repetitions; cardio and timed sets have no repetitions.
            Volume = g.Sum(s => s.WeightKg * s.Reps),
            MonthVolume = g.Where(s => s.PerformedExercise.WorkoutSession.SessionDate >= monthAgo).Sum(s => s.WeightKg * s.Reps),
            WeekVolume = g.Where(s => s.PerformedExercise.WorkoutSession.SessionDate >= weekAgo).Sum(s => s.WeightKg * s.Reps)
        }).SingleOrDefaultAsync(cancellationToken);
        var favorite = await performed
            .GroupBy(pe => new { pe.ExerciseId, pe.Exercise.Name, pe.Exercise.NameUk })
            .Select(g => new { g.Key.Name, g.Key.NameUk, Sessions = g.Select(pe => pe.WorkoutSessionId).Distinct().Count() })
            .OrderByDescending(x => x.Sessions).ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var sessionDates = await sessions.Select(s => s.SessionDate).ToListAsync(cancellationToken);
        var uniqueExercises = await performed.Select(pe => pe.ExerciseId).Distinct().CountAsync(cancellationToken);
        var exercisesThisMonth = await performed.Where(pe => pe.WorkoutSession.SessionDate >= monthAgo)
            .Select(pe => pe.ExerciseId).Distinct().CountAsync(cancellationToken);
        var awards = await db.UserAchievements.CountAsync(ua => ua.UserId == userId, cancellationToken);
        var records = await RecordsAsync(userId, cancellationToken);
        var workouts = sessionTotals?.Count ?? 0;
        var setCount = setTotals?.Count ?? 0;
        // Weeks since the first workout, at least one, so a new journal is not divided by a fraction.
        var weeksActive = workouts > 0 ? Math.Max(1, (now - sessionTotals!.First).TotalDays / 7) : 1;
        var bestProgress = await BestLoadChangeAsync(userId, cancellationToken);

        return new TrainingStatistics
        {
            WorkoutsCompleted = workouts,
            TotalWorkoutMinutes = sessionTotals?.Minutes ?? 0,
            AvgWorkoutDurationMinutes = workouts > 0 ? Math.Round(sessionTotals!.Minutes / (double)workouts, 1) : 0,
            LongestStreak = AthleteCalendar.LongestStreak(sessionDates.Select(calendar.DayOf)),
            AvgWeeklyWorkouts = Math.Round(workouts / weeksActive, 1),
            TotalSetsCount = setCount,
            AvgSetsPerWorkout = workouts > 0 ? Math.Round(setCount / (double)workouts, 1) : 0,
            AvgRestSeconds = setCount > 0 ? (int)setTotals!.AverageRest : 0,
            TotalVolume = Math.Round(setTotals?.Volume ?? 0, 1),
            MonthlyTotalTonnage = Math.Round(setTotals?.MonthVolume ?? 0, 1),
            WeeklyProgress = Math.Round(setTotals?.WeekVolume ?? 0, 1),
            Last7DaysWorkouts = sessionTotals?.WeekCount ?? 0,
            Last7DaysMinutes = sessionTotals?.WeekMinutes ?? 0,
            UniqueExercisesCount = uniqueExercises,
            ExercisesThisMonth = exercisesThisMonth,
            FavoriteExerciseName = favorite is null ? null : Localized.Pick(favorite.Name, favorite.NameUk),
            FavoriteExerciseCount = favorite?.Sessions ?? 0,
            BestProgressExercise = bestProgress.ExerciseName,
            BestProgressPercent = Math.Round(bestProgress.ProgressPercent, 1),
            TotalAwardsCount = awards,
            RecentRecords = records.OrderByDescending(r => r.AchievedAt).Take(10).ToList()
        };
    }

    public async Task<List<PersonalRecordItem>> RecordsAsync(int userId, CancellationToken cancellationToken)
    {
        var records = await db.PersonalRecords.AsNoTracking()
            .Where(pr => pr.UserId == userId)
            .Select(pr => new PersonalRecordItem
            {
                MuscleGroupName = Localized.Pick(pr.Exercise.MuscleSubGroup.MuscleGroup.Name, pr.Exercise.MuscleSubGroup.MuscleGroup.NameUk),
                MuscleSubGroupName = Localized.Pick(pr.Exercise.MuscleSubGroup.Name, pr.Exercise.MuscleSubGroup.NameUk),
                ExerciseName = Localized.Pick(pr.Exercise.Name, pr.Exercise.NameUk),
                MaxWeightKg = pr.MaxWeightKg,
                AchievedAt = pr.AchievedAt
            })
            .ToListAsync(cancellationToken);
        return records
            .OrderBy(pr => pr.MuscleGroupName, Localized.Order)
            .ThenBy(pr => pr.MuscleSubGroupName, Localized.Order)
            .ThenBy(pr => pr.ExerciseName, Localized.Order)
            .ToList();
    }

    /// <summary>Largest load change between the first and latest logged set at the same repetitions.</summary>
    private async Task<(string? ExerciseName, double ProgressPercent)> BestLoadChangeAsync(int userId, CancellationToken cancellationToken)
    {
        var candidates = await db.ExerciseProgress.AsNoTracking()
            .Where(ep => ep.UserId == userId && ep.Exercise.Type == ExerciseType.StrengthTraining
                && ep.CurrentWeightKg > 0 && ep.InitialWeightKg > 0 && ep.InitialReps > 0 && ep.CurrentReps == ep.InitialReps)
            .Include(ep => ep.Exercise)
            .ToListAsync(cancellationToken);
        var best = candidates
            .Select(ep => (Name: (string?)Localized.Pick(ep.Exercise.Name, ep.Exercise.NameUk), Progress: ep.WeightChangePercent))
            .OrderByDescending(x => x.Progress)
            .FirstOrDefault();
        return best.Name is null ? (null, 0) : best;
    }
}
