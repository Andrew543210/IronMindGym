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
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var today = DateTime.UtcNow.Date;
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == currentUserId && n.Date == today);

        var vm = new NutritionViewModel();
        vm.RecentLogs = BuildRecentLogs(currentUserId);
        if (log is not null)
        {
            vm.CaloriesKcal = log.CaloriesKcal;
            vm.ProteinG = log.ProteinG;
            vm.FatG = log.FatG;
            vm.CarbsG = log.CarbsG;

            var latestWeight = db.AnthropometryLogs
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.RecordedAt)
                .Select(x => (double?)x.WeightKg)
                .FirstOrDefault();

            var recentLogs = db.DailyNutritions
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.Date)
                .Take(14)
                .ToList();

            var advice = adviceService.BuildAdvice(log, latestWeight, recentLogs);
            vm.SystemAdvice = advice.Summary;
            vm.SystemVerdict = advice.Verdict;
            vm.SystemScore = advice.Score;
            vm.AdviceItems = advice.Tips;
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult Index(NutritionViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
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

        var latestWeightAfterSave = db.AnthropometryLogs
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.RecordedAt)
            .Select(x => (double?)x.WeightKg)
            .FirstOrDefault();

        var recentLogsAfterSave = db.DailyNutritions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .Take(14)
            .ToList();

        var adviceResult = adviceService.BuildAdvice(log, latestWeightAfterSave, recentLogsAfterSave);
        model.SystemAdvice = adviceResult.Summary;
        model.SystemVerdict = adviceResult.Verdict;
        model.SystemScore = adviceResult.Score;
        model.AdviceItems = adviceResult.Tips;
        model.RecentLogs = BuildRecentLogs(currentUserId);

        TempData["Message"] = "Харчовий запис оновлено.";

        return View(model);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
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

        var latestWeight = db.AnthropometryLogs
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.RecordedAt)
            .Select(x => (double?)x.WeightKg)
            .FirstOrDefault();

        var recentLogs = db.DailyNutritions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .Take(14)
            .ToList();

        var advice = adviceService.BuildAdvice(log, latestWeight, recentLogs);

        var vm = new NutritionDetailsViewModel
        {
            Log = new NutritionLogListItemViewModel
            {
                Id = log.Id,
                Date = log.Date,
                CaloriesKcal = log.CaloriesKcal,
                ProteinG = log.ProteinG,
                FatG = log.FatG,
                CarbsG = log.CarbsG
            },
            SystemAdvice = advice.Summary,
            SystemVerdict = advice.Verdict,
            SystemScore = advice.Score,
            AdviceItems = advice.Tips,
            RecentLogs = BuildRecentLogs(currentUserId)
        };

        return View(vm);
    }

    private List<NutritionLogListItemViewModel> BuildRecentLogs(int userId)
    {
        return db.DailyNutritions
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Date)
            .Take(10)
            .Select(n => new NutritionLogListItemViewModel
            {
                Id = n.Id,
                Date = n.Date,
                CaloriesKcal = n.CaloriesKcal,
                ProteinG = n.ProteinG,
                FatG = n.FatG,
                CarbsG = n.CarbsG
            })
            .ToList();
    }
}
