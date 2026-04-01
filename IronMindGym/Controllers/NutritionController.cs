using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class NutritionController(IronMindDbContext db, INutritionAdviceService adviceService) : Controller
{
    private const int DemoUserId = 1;

    [HttpGet]
    public IActionResult Index()
    {
        var today = DateTime.UtcNow.Date;
        var log = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == DemoUserId && n.Date == today);

        var vm = new NutritionViewModel();
        if (log is not null)
        {
            vm.CaloriesKcal = log.CaloriesKcal;
            vm.ProteinG = log.ProteinG;
            vm.FatG = log.FatG;
            vm.CarbsG = log.CarbsG;
            vm.SystemAdvice = adviceService.BuildAdvice(log);
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult Index(NutritionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var today = DateTime.UtcNow.Date;
        var log = db.DailyNutritions.FirstOrDefault(n => n.UserId == DemoUserId && n.Date == today);

        if (log is null)
        {
            log = new DailyNutrition { UserId = DemoUserId, Date = today };
            db.DailyNutritions.Add(log);
        }

        log.CaloriesKcal = model.CaloriesKcal;
        log.ProteinG = model.ProteinG;
        log.FatG = model.FatG;
        log.CarbsG = model.CarbsG;

        model.SystemAdvice = adviceService.BuildAdvice(log);

        db.SaveChanges();
        TempData["Message"] = "Харчовий запис оновлено.";

        return View(model);
    }
}

