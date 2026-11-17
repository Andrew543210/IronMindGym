using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class NutritionController(IronMindDbContext db, INutritionAdviceService adviceService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var today = DateTime.UtcNow.Date;
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == currentUserId && n.Date == today);
        var recentLogs = db.DailyNutritions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .Take(14)
            .ToList();
        var profile = BuildNutritionProfile(currentUserId);

        var vm = new NutritionViewModel();
        vm.RecentLogs = BuildRecentLogs(currentUserId);
        if (log is not null)
        {
            vm.CaloriesKcal = log.CaloriesKcal;
            vm.ProteinG = log.ProteinG;
            vm.FatG = log.FatG;
            vm.CarbsG = log.CarbsG;

            var advice = adviceService.BuildAdvice(log, profile, recentLogs);
            ApplyAdvice(vm, advice);
        }
        else
        {
            var previewAdvice = adviceService.BuildAdvice(new DailyNutrition { Date = today }, profile, recentLogs);
            vm.TargetCaloriesKcal = previewAdvice.TargetCaloriesKcal;
            vm.TargetProteinG = previewAdvice.TargetProteinG;
            vm.TargetFatG = previewAdvice.TargetFatG;
            vm.TargetCarbsG = previewAdvice.TargetCarbsG;
            vm.BasalMetabolicRate = previewAdvice.BasalMetabolicRate;
            vm.ActivityMultiplier = previewAdvice.ActivityMultiplier;
            vm.BmrFormulaText = previewAdvice.BmrFormulaText;
            vm.CaloriesFormulaText = previewAdvice.CaloriesFormulaText;
            vm.ProteinFormulaText = previewAdvice.ProteinFormulaText;
            vm.FatFormulaText = previewAdvice.FatFormulaText;
            vm.CarbsFormulaText = previewAdvice.CarbsFormulaText;
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult Index(NutritionViewModel model)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            var profile = BuildNutritionProfile(userId.Value);
            var recentLogs = db.DailyNutritions
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .OrderByDescending(x => x.Date)
                .Take(14)
                .ToList();
            var previewAdvice = adviceService.BuildAdvice(new DailyNutrition
            {
                Date = DateTime.UtcNow.Date,
                CaloriesKcal = model.CaloriesKcal,
                ProteinG = model.ProteinG,
                FatG = model.FatG,
                CarbsG = model.CarbsG
            }, profile, recentLogs);

            model.TargetCaloriesKcal = previewAdvice.TargetCaloriesKcal;
            model.TargetProteinG = previewAdvice.TargetProteinG;
            model.TargetFatG = previewAdvice.TargetFatG;
            model.TargetCarbsG = previewAdvice.TargetCarbsG;
            model.BasalMetabolicRate = previewAdvice.BasalMetabolicRate;
            model.ActivityMultiplier = previewAdvice.ActivityMultiplier;
            model.BmrFormulaText = previewAdvice.BmrFormulaText;
            model.CaloriesFormulaText = previewAdvice.CaloriesFormulaText;
            model.ProteinFormulaText = previewAdvice.ProteinFormulaText;
            model.FatFormulaText = previewAdvice.FatFormulaText;
            model.CarbsFormulaText = previewAdvice.CarbsFormulaText;
            model.RecentLogs = BuildRecentLogs(userId.Value);
            return View(model);
        }

        var currentUserId = userId.Value;
        var today = DateTime.UtcNow.Date;
        var log = db.DailyNutritions.FirstOrDefault(n => n.UserId == currentUserId && n.Date == today);

        if (log is null)
        {
            log = new DailyNutrition { UserId = currentUserId, Date = today };
            db.DailyNutritions.Add(log);
        }

        log.CaloriesKcal = model.CaloriesKcal;
        log.ProteinG = model.ProteinG;
        log.FatG = model.FatG;
        log.CarbsG = model.CarbsG;

        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, currentUserId);

        var recentLogsAfterSave = db.DailyNutritions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .Take(14)
            .ToList();

        var profileAfterSave = BuildNutritionProfile(currentUserId);
        var adviceResult = adviceService.BuildAdvice(log, profileAfterSave, recentLogsAfterSave);
        ApplyAdvice(model, adviceResult);
        model.RecentLogs = BuildRecentLogs(currentUserId);

        TempData["Message"] = "Nutrition log has been updated.";

        return View(model);
    }

    [HttpGet]
    public IActionResult EditToday(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var today = DateTime.UtcNow.Date;
        var editable = db.DailyNutritions.AsNoTracking().Any(x => x.Id == id && x.UserId == userId.Value && x.Date == today);
        if (!editable)
        {
            TempData["Message"] = "Only today's nutrition entry can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Message"] = "Editing today's nutrition entry.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var log = db.DailyNutritions
            .AsNoTracking()
            .FirstOrDefault(n => n.UserId == currentUserId && n.Id == id);

        if (log is null)
        {
            return NotFound();
        }

        var recentLogs = db.DailyNutritions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .Take(14)
            .ToList();

        var profile = BuildNutritionProfile(currentUserId);
        var advice = adviceService.BuildAdvice(log, profile, recentLogs);

        var vm = new NutritionDetailsViewModel
        {
            Log = new NutritionLogListItemViewModel
            {
                Id = log.Id,
                Date = log.Date,
                IsToday = log.Date == DateTime.UtcNow.Date,
                CaloriesKcal = log.CaloriesKcal,
                ProteinG = log.ProteinG,
                FatG = log.FatG,
                CarbsG = log.CarbsG
            },
            SystemAdvice = advice.Summary,
            SystemVerdict = advice.Verdict,
            SystemScore = advice.Score,
            AdviceItems = advice.Tips,
            TargetCaloriesKcal = advice.TargetCaloriesKcal,
            TargetProteinG = advice.TargetProteinG,
            TargetFatG = advice.TargetFatG,
            TargetCarbsG = advice.TargetCarbsG,
            BasalMetabolicRate = advice.BasalMetabolicRate,
            ActivityMultiplier = advice.ActivityMultiplier,
            BmrFormulaText = advice.BmrFormulaText,
            CaloriesFormulaText = advice.CaloriesFormulaText,
            ProteinFormulaText = advice.ProteinFormulaText,
            FatFormulaText = advice.FatFormulaText,
            CarbsFormulaText = advice.CarbsFormulaText,
            RecentLogs = BuildRecentLogs(currentUserId)
        };

        return View(vm);
    }

    private List<NutritionLogListItemViewModel> BuildRecentLogs(int userId)
    {
        var today = DateTime.UtcNow.Date;
        return db.DailyNutritions
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Date)
            .Take(10)
            .Select(n => new NutritionLogListItemViewModel
            {
                Id = n.Id,
                Date = n.Date,
                IsToday = n.Date == today,
                CaloriesKcal = n.CaloriesKcal,
                ProteinG = n.ProteinG,
                FatG = n.FatG,
                CarbsG = n.CarbsG
            })
            .ToList();
    }

    private NutritionProfileSnapshot BuildNutritionProfile(int userId)
    {
        var latestAnthro = db.AnthropometryLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RecordedAt)
            .Select(x => new { x.WeightKg, x.HeightCm })
            .FirstOrDefault();

        var profile = db.Profiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Age })
            .FirstOrDefault();

        var workoutsPerWeek = db.WorkoutPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.WorkoutsPerWeek)
            .FirstOrDefault();

        return new NutritionProfileSnapshot
        {
            WeightKg = latestAnthro?.WeightKg,
            HeightCm = latestAnthro?.HeightCm,
            Age = profile?.Age,
            WorkoutsPerWeek = workoutsPerWeek
        };
    }

    private static void ApplyAdvice(NutritionViewModel model, NutritionAdviceResult advice)
    {
        model.SystemAdvice = advice.Summary;
        model.SystemVerdict = advice.Verdict;
        model.SystemScore = advice.Score;
        model.AdviceItems = advice.Tips;
        model.TargetCaloriesKcal = advice.TargetCaloriesKcal;
        model.TargetProteinG = advice.TargetProteinG;
        model.TargetFatG = advice.TargetFatG;
        model.TargetCarbsG = advice.TargetCarbsG;
        model.BasalMetabolicRate = advice.BasalMetabolicRate;
        model.ActivityMultiplier = advice.ActivityMultiplier;
        model.BmrFormulaText = advice.BmrFormulaText;
        model.CaloriesFormulaText = advice.CaloriesFormulaText;
        model.ProteinFormulaText = advice.ProteinFormulaText;
        model.FatFormulaText = advice.FatFormulaText;
        model.CarbsFormulaText = advice.CarbsFormulaText;
    }
}
