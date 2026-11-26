using System.ComponentModel.DataAnnotations;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>One entry per calendar day. Saving a day again replaces it, so a missed day can be filled in later.</summary>
public class NutritionJournalService(IronMindDbContext db, NutritionAdviceService advice, CurrentCalendar calendar)
{
    private const int RecentDays = 30;

    public NutritionDay Day(int userId, DateTime date)
    {
        var day = date.Date;
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == userId && n.Date == day)
            ?? new DailyNutrition { UserId = userId, Date = day };
        var recent = db.DailyNutritions.AsNoTracking().Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Date).Take(RecentDays).ToList();
        var week = db.DailyNutritions.AsNoTracking()
            .Where(n => n.UserId == userId && n.Date <= day && n.Date >= day.AddDays(-6)).ToList();
        return new NutritionDay(
            day,
            log.Id != 0,
            log.CaloriesKcal,
            log.ProteinG,
            log.FatG,
            log.CarbsG,
            advice.BuildAdvice(log, Snapshot(userId), week),
            recent.Select(n => new NutritionLogEntry(n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG)).ToList());
    }

    public void Save(int userId, DateTime date, double caloriesKcal, double proteinG, double fatG, double carbsG)
    {
        var day = date.Date;
        if (day > calendar.Today || day < calendar.Today.AddYears(-5))
            throw new ValidationException("Choose a date within the last five years, up to today.");
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        var log = db.DailyNutritions.FirstOrDefault(n => n.UserId == userId && n.Date == day);
        if (log is null)
            db.DailyNutritions.Add(log = new DailyNutrition { UserId = userId, Date = day });
        log.CaloriesKcal = caloriesKcal;
        log.ProteinG = proteinG;
        log.FatG = fatG;
        log.CarbsG = carbsG;
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
        transaction.Commit();
    }

    public bool Delete(int userId, DateTime date)
    {
        var day = date.Date;
        return db.DailyNutritions.Where(n => n.UserId == userId && n.Date == day).ExecuteDelete() > 0;
    }

    private NutritionProfileSnapshot Snapshot(int userId)
    {
        var profile = db.Profiles.AsNoTracking().FirstOrDefault(p => p.UserId == userId);
        var body = db.AnthropometryLogs.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();
        return new NutritionProfileSnapshot
        {
            WeightKg = body?.WeightKg,
            HeightCm = body?.HeightCm,
            Age = profile?.Age,
            CalorieTarget = profile?.CalorieTarget,
            ProteinTarget = profile?.ProteinTarget,
            FatTarget = profile?.FatTarget,
            CarbsTarget = profile?.CarbsTarget,
            EnergyEquation = profile?.EnergyEquation ?? "None",
            WorkoutsPerWeek = db.WorkoutPlans.Where(p => p.UserId == userId && p.IsActive).Select(p => p.WorkoutsPerWeek).FirstOrDefault()
        };
    }
}
