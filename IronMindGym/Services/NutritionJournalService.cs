using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>One entry per calendar day. Saving a day again replaces it, so a missed day can be filled in later.</summary>
public sealed class NutritionJournalService(IronMindDbContext db, NutritionAdviceService advice, AthleteCalendars calendars, AchievementService achievements)
{
    private const int RecentDays = 30;

    /// <summary>The day with its advice and the recent log; without a date, the athlete's today.</summary>
    public async Task<NutritionDay> DayAsync(int userId, DateOnly? date, CancellationToken cancellationToken)
    {
        var day = date ?? (await calendars.ForAsync(userId, cancellationToken)).Today;
        var log = await db.DailyNutritions.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == userId && n.Date == day, cancellationToken)
            ?? new DailyNutrition { UserId = userId, Date = day };
        var recent = await db.DailyNutritions.AsNoTracking().Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Date).Take(RecentDays).ToListAsync(cancellationToken);
        var week = await db.DailyNutritions.AsNoTracking()
            .Where(n => n.UserId == userId && n.Date <= day && n.Date >= day.AddDays(-6)).ToListAsync(cancellationToken);
        return new NutritionDay(
            day,
            log.Id != 0,
            log.CaloriesKcal,
            log.ProteinG,
            log.FatG,
            log.CarbsG,
            advice.BuildAdvice(log, await SnapshotAsync(userId, cancellationToken), week),
            recent.Select(n => new NutritionLogEntry(n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG)).ToList());
    }

    public async Task SaveAsync(int userId, DateOnly day, NutritionEntryRequest entry, CancellationToken cancellationToken)
    {
        var today = (await calendars.ForAsync(userId, cancellationToken)).Today;
        if (day > today || day < today.AddYears(-5))
            throw new DomainValidationException("Choose a date within the last five years, up to today.", "Date");
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var log = await db.DailyNutritions.FirstOrDefaultAsync(n => n.UserId == userId && n.Date == day, cancellationToken);
        if (log is null)
            db.DailyNutritions.Add(log = new DailyNutrition { UserId = userId, Date = day });
        log.CaloriesKcal = entry.CaloriesKcal;
        log.ProteinG = entry.ProteinG;
        log.FatG = entry.FatG;
        log.CarbsG = entry.CarbsG;
        await db.SaveChangesAsync(cancellationToken);
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int userId, DateOnly day, CancellationToken cancellationToken) =>
        await db.DailyNutritions.Where(n => n.UserId == userId && n.Date == day).ExecuteDeleteAsync(cancellationToken) > 0;

    private async Task<NutritionProfileSnapshot> SnapshotAsync(int userId, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return new NutritionProfileSnapshot
        {
            WeightKg = (await db.WeightLogs.LatestWeighInAsync(userId, cancellationToken))?.ValueKg,
            HeightCm = profile?.HeightCm,
            Age = profile?.Age,
            CalorieTarget = profile?.CalorieTarget,
            ProteinTarget = profile?.ProteinTarget,
            FatTarget = profile?.FatTarget,
            CarbsTarget = profile?.CarbsTarget,
            EnergyEquation = profile?.EnergyEquation ?? EnergyEquation.None,
            WorkoutsPerWeek = await db.WorkoutPlans.WorkoutsPerWeekAsync(userId, cancellationToken)
        };
    }
}
