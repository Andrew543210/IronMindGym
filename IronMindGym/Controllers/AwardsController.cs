using IronMindGym.Data;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Controllers;

public class AwardsController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        AchievementEngine.AssignAchievements(db, userId.Value);
        var progress = AchievementEngine.BuildAwardsProgress(db, userId.Value);

        var vm = new AwardsViewModel
        {
            Awards = progress.Select(x => new AwardProgressViewModel
            {
                Code = x.Code,
                Name = x.Name,
                ConditionDescription = x.ConditionDescription,
                IsUnlocked = x.IsUnlocked,
                IsNumeric = x.IsNumeric,
                CurrentValue = x.CurrentValue,
                TargetValue = x.TargetValue,
                ProgressPercent = x.ProgressPercent
            }).ToList()
        };

        return View(vm);
    }
}

