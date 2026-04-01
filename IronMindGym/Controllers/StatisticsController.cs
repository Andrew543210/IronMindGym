using IronMindGym.Data;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class StatisticsController(IronMindDbContext db) : Controller
{
    private const int DemoUserId = 1;

    public IActionResult Index()
    {
        var sessions = db.WorkoutSessions
            .Where(s => s.UserId == DemoUserId)
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Sets)
            .ToList();

        var monthlyTonnage = sessions
            .Where(s => s.SessionDate >= DateTime.UtcNow.AddDays(-30))
            .SelectMany(s => s.PerformedExercises)
            .SelectMany(pe => pe.Sets)
            .Sum(set => set.WeightKg * set.Reps);

        var nutrition = db.DailyNutritions
            .Where(n => n.UserId == DemoUserId && n.Date >= DateTime.UtcNow.AddDays(-7))
            .ToList();

        var personalRecords = db.PersonalRecords
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == DemoUserId)
            .AsNoTracking()
            .ToList();

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
                .ToList()
        };

        return View(vm);
    }
}
