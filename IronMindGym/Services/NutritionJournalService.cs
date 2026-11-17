using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class NutritionJournalService(IronMindDbContext db, INutritionAdviceService advice)
{
    public NutritionViewModel Build(int userId, DateTime date)
    {
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == userId && n.Date == date.Date);
        var model = new NutritionViewModel
        {
            Date = date.Date,
            CaloriesKcal = log?.CaloriesKcal ?? 0,
            ProteinG = log?.ProteinG ?? 0,
            FatG = log?.FatG ?? 0,
            CarbsG = log?.CarbsG ?? 0
        };
        Populate(userId, model);
        return model;
    }

    public void Populate(int userId, NutritionViewModel model)
    {
        var log = new DailyNutrition { Date = model.Date.Date, CaloriesKcal = model.CaloriesKcal, ProteinG = model.ProteinG, FatG = model.FatG, CarbsG = model.CarbsG };
        Apply(model, Evaluate(userId, log));
        model.RecentLogs = Recent(userId);
    }

    public NutritionDetailsViewModel? Details(int userId, int id)
    {
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.Id == id && n.UserId == userId);
        if (log is null)
            return null;
        var model = new NutritionDetailsViewModel { Log = Map(log), RecentLogs = Recent(userId) };
        Apply(model, Evaluate(userId, log));
        return model;
    }

    public void Save(int userId, NutritionViewModel model)
    {
        if (model.Date.Date > DateTime.UtcNow.Date || model.Date.Date < DateTime.UtcNow.Date.AddYears(-5))
            throw new ValidationException("Choose a date within the last five years, up to today.");
        using var transaction = db.Database.BeginTransaction();
        var log = db.DailyNutritions.FirstOrDefault(n => n.UserId == userId && n.Date == model.Date.Date);
        if (log is null)
        {
            log = new DailyNutrition { UserId = userId, Date = model.Date.Date };
            db.DailyNutritions.Add(log);
        }
        log.CaloriesKcal = model.CaloriesKcal;
        log.ProteinG = model.ProteinG;
        log.FatG = model.FatG;
        log.CarbsG = model.CarbsG;
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
        transaction.Commit();
    }

    public bool Delete(int userId, int id)
    {
        var log = db.DailyNutritions.FirstOrDefault(n => n.UserId == userId && n.Id == id);
        if (log is null)
            return false;
        db.DailyNutritions.Remove(log);
        db.SaveChanges();
        return true;
    }

    private NutritionAdviceResult Evaluate(int userId, DailyNutrition log)
    {
        var profile = db.Profiles.AsNoTracking().FirstOrDefault(p => p.UserId == userId);
        var body = db.AnthropometryLogs.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();
        var snapshot = new NutritionProfileSnapshot
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
        var recent = db.DailyNutritions.AsNoTracking().Where(n => n.UserId == userId && n.Date <= log.Date && n.Date >= log.Date.AddDays(-6)).ToList();
        return advice.BuildAdvice(log, snapshot, recent);
    }

    private List<NutritionLogListItemViewModel> Recent(int userId) => db.DailyNutritions.AsNoTracking()
        .Where(n => n.UserId == userId).OrderByDescending(n => n.Date).Take(30).ToList().Select(Map).ToList();

    private static NutritionLogListItemViewModel Map(DailyNutrition log) => new()
    {
        Id = log.Id,
        Date = log.Date,
        IsToday = log.Date == DateTime.UtcNow.Date,
        CaloriesKcal = log.CaloriesKcal,
        ProteinG = log.ProteinG,
        FatG = log.FatG,
        CarbsG = log.CarbsG
    };

    private static void Apply(NutritionTargetsViewModel model, NutritionAdviceResult result)
    {
        model.SystemAdvice = result.Summary;
        model.SystemVerdict = result.Verdict;
        model.SystemScore = result.Score;
        model.AdviceItems = result.Tips;
        model.TargetCaloriesKcal = result.TargetCaloriesKcal;
        model.TargetProteinG = result.TargetProteinG;
        model.TargetFatG = result.TargetFatG;
        model.TargetCarbsG = result.TargetCarbsG;
        model.BasalMetabolicRate = result.BasalMetabolicRate;
        model.ActivityMultiplier = result.ActivityMultiplier;
        model.BmrFormulaText = result.BmrFormulaText;
        model.CaloriesFormulaText = result.CaloriesFormulaText;
        model.ProteinFormulaText = result.ProteinFormulaText;
        model.FatFormulaText = result.FatFormulaText;
        model.CarbsFormulaText = result.CarbsFormulaText;
    }
}
