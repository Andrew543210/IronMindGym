using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class StatisticsController(IronMindDbContext db) : Controller
{
    public IActionResult Index()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var sessions = db.WorkoutSessions
            .Where(s => s.UserId == currentUserId)
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Sets)
            .ToList();

        var monthlyTonnage = sessions
            .Where(s => s.SessionDate >= DateTime.UtcNow.AddDays(-30))
            .SelectMany(s => s.PerformedExercises)
            .SelectMany(pe => pe.Sets)
            .Sum(set => set.WeightKg * set.Reps);

        var nutrition = db.DailyNutritions
            .Where(n => n.UserId == currentUserId && n.Date >= DateTime.UtcNow.AddDays(-7))
            .ToList();

        var personalRecords = db.PersonalRecords
            .Include(pr => pr.Exercise)
            .ThenInclude(e => e.MuscleSubGroup)
            .ThenInclude(sg => sg.MuscleGroup)
            .Where(pr => pr.UserId == currentUserId)
            .AsNoTracking()
            .ToList();

        // Calculate new statistics
        var totalWorkoutMinutes = sessions.Sum(s => s.DurationMinutes);
        var avgWorkoutDuration = sessions.Count > 0 ? totalWorkoutMinutes / (double)sessions.Count : 0;
        
        // Longest streak
        var longestStreak = CalculateLongestStreak(sessions);
        
        // Best progress from ExerciseProgress
        var bestProgress = CalculateBestProgress(currentUserId);
        
        // Average rest seconds
        var allSets = sessions.SelectMany(s => s.PerformedExercises).SelectMany(pe => pe.Sets).ToList();
        var avgRestSeconds = allSets.Count > 0 ? (int)allSets.Average(s => s.RestSeconds) : 0;
        
        // Count awards
        var awardsCount = db.UserAchievements
            .Where(ua => ua.UserId == currentUserId)
            .Count();

        // ADDITIONAL USEFUL STATISTICS
        // 1. Total Volume (kg × reps)
        var totalVolume = allSets.Sum(s => s.WeightKg * s.Reps);
        
        // 2. Unique exercises count
        var uniqueExercisesCount = sessions
            .SelectMany(s => s.PerformedExercises)
            .Select(pe => pe.ExerciseId)
            .Distinct()
            .Count();
        
        // 3. Exercises in last 30 days
        var exercisesThisMonth = sessions
            .Where(s => s.SessionDate >= DateTime.UtcNow.AddDays(-30))
            .SelectMany(s => s.PerformedExercises)
            .Select(pe => pe.ExerciseId)
            .Distinct()
            .Count();
        
        // 4. Average workouts per week
        var weeksActive = sessions.Count > 0 
            ? (DateTime.UtcNow - sessions.Min(s => s.SessionDate)).TotalDays / 7 
            : 1;
        var avgWeeklyWorkouts = weeksActive > 0 ? sessions.Count / weeksActive : 0;
        
        // 5. Total sets count
        var totalSetsCount = allSets.Count;
        
        // 6. Average sets per workout
        var avgSetsPerWorkout = sessions.Count > 0 ? totalSetsCount / (double)sessions.Count : 0;
        
        // 7. Favorite exercise (most performed)
        var favoriteExercise = sessions
            .SelectMany(s => s.PerformedExercises)
            .GroupBy(pe => pe.ExerciseId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var favoriteExerciseName = "N/A";
        var favoriteExerciseCount = 0;
        if (favoriteExercise != null)
        {
            var ex = db.Exercises.FirstOrDefault(e => e.Id == favoriteExercise.Key);
            favoriteExerciseName = ex?.Name ?? "N/A";
            favoriteExerciseCount = favoriteExercise.Count();
        }
        
        // 8. Weekly progress (last 7 days)
        var last7DaysSessions = sessions.Where(s => s.SessionDate >= DateTime.UtcNow.AddDays(-7)).ToList();
        var weeklyProgress = last7DaysSessions.Count > 0 
            ? last7DaysSessions.Sum(s => s.PerformedExercises.SelectMany(pe => pe.Sets).Sum(st => st.WeightKg * st.Reps))
            : 0;
        
        // 9. Monthly progress (last 30 days)
        var last30DaysSessions = sessions.Where(s => s.SessionDate >= DateTime.UtcNow.AddDays(-30)).ToList();
        var monthlyProgress = last30DaysSessions.Count > 0
            ? last30DaysSessions.Sum(s => s.PerformedExercises.SelectMany(pe => pe.Sets).Sum(st => st.WeightKg * st.Reps))
            : 0;
        
        // 10. Last 7 days info
        var last7DaysInfo = last7DaysSessions.Count > 0
            ? $"{last7DaysSessions.Count} workouts, {last7DaysSessions.Sum(s => s.DurationMinutes)} min"
            : "No workouts";

        var vm = new StatisticsViewModel
        {
            WorkoutsCompleted = sessions.Count,
            MonthlyTotalTonnage = Math.Round(monthlyTonnage, 1),
            AvgWeeklyCalories = nutrition.Count > 0 ? Math.Round(nutrition.Average(n => n.CaloriesKcal), 1) : 0,
            ProteinGoalAdherencePercent = nutrition.Count > 0
                ? Math.Round(nutrition.Count(n => n.ProteinG >= 120) * 100.0 / nutrition.Count, 1)
                : 0,
            PersonalRecords = personalRecords
                .Select(pr => (pr.Exercise.Name, pr.MaxWeightKg))
                .ToList(),
            PersonalRecordItems = personalRecords
                .OrderBy(pr => pr.Exercise.MuscleSubGroup.MuscleGroup.Name)
                .ThenBy(pr => pr.Exercise.MuscleSubGroup.Name)
                .ThenBy(pr => pr.Exercise.Name)
                .Select(pr => new PersonalRecordListItemViewModel
                {
                    MuscleGroupName = pr.Exercise.MuscleSubGroup.MuscleGroup.Name,
                    MuscleSubGroupName = pr.Exercise.MuscleSubGroup.Name,
                    ExerciseName = pr.Exercise.Name,
                    MaxWeightKg = pr.MaxWeightKg,
                    UpdatedAt = pr.UpdatedAt
                })
                .ToList(),
            
            // New statistics
            TotalWorkoutMinutes = totalWorkoutMinutes,
            AvgWorkoutDurationMinutes = Math.Round(avgWorkoutDuration, 1),
            LongestStreak = longestStreak,
            BestProgressPercent = Math.Round(bestProgress.ProgressPercent, 1),
            BestProgressExercise = bestProgress.ExerciseName,
            AvgRestSeconds = avgRestSeconds,
            TotalAwardsCount = awardsCount,
            
            // Additional useful statistics
            TotalVolume = Math.Round(totalVolume, 1),
            UniquExercisesCount = uniqueExercisesCount,
            ExercisesThisMonth = exercisesThisMonth,
            AvgWeeklyWorkouts = Math.Round(avgWeeklyWorkouts, 1),
            TotalSetsCount = totalSetsCount,
            AvgSetsPerWorkout = Math.Round(avgSetsPerWorkout, 1),
            FavoriteExerciseName = favoriteExerciseName,
            FavoriteExerciseCount = favoriteExerciseCount,
            WeeklyProgress = Math.Round(weeklyProgress, 1),
            MonthlyProgress = Math.Round(monthlyProgress, 1),
            Last7DaysInfo = last7DaysInfo
        };

        return View(vm);
    }
    
    private int CalculateLongestStreak(List<WorkoutSession> sessions)
    {
        if (sessions.Count == 0) return 0;
        
        var dates = sessions.OrderBy(s => s.SessionDate).Select(s => s.SessionDate.Date).Distinct().ToList();
        if (dates.Count == 0) return 0;
        
        int maxStreak = 1;
        int currentStreak = 1;
        
        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i] - dates[i - 1]).TotalDays == 1)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }
        
        return maxStreak;
    }
    
    private (string ExerciseName, double ProgressPercent) CalculateBestProgress(int userId)
    {
        var progress = db.ExerciseProgress
            .Where(ep => ep.UserId == userId && ep.CurrentWeightKg > 0 && ep.InitialWeightKg > 0)
            .Include(ep => ep.Exercise)
            .AsNoTracking()
            .ToList();
        
        if (progress.Count == 0)
            return ("N/A", 0);
        
        var best = progress
            .Select(ep => new
            {
                Name = ep.Exercise.Name,
                Progress = ep.CalculateWeightProgressPercent()
            })
            .OrderByDescending(x => x.Progress)
            .FirstOrDefault();
        
        return (best?.Name ?? "N/A", best?.Progress ?? 0);
    }

    [HttpGet]
    public IActionResult Records()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var records = db.PersonalRecords
            .Where(pr => pr.UserId == currentUserId)
            .Include(pr => pr.Exercise)
            .ThenInclude(e => e.MuscleSubGroup)
            .ThenInclude(ms => ms.MuscleGroup)
            .AsNoTracking()
            .OrderBy(pr => pr.Exercise.MuscleSubGroup.MuscleGroup.Name)
            .ThenBy(pr => pr.Exercise.MuscleSubGroup.Name)
            .ThenBy(pr => pr.Exercise.Name)
            .Select(pr => new PersonalRecordListItemViewModel
            {
                MuscleGroupName = pr.Exercise.MuscleSubGroup.MuscleGroup.Name,
                MuscleSubGroupName = pr.Exercise.MuscleSubGroup.Name,
                ExerciseName = pr.Exercise.Name,
                MaxWeightKg = pr.MaxWeightKg,
                UpdatedAt = pr.UpdatedAt
            })
            .ToList();

        return View(records);
    }
}
