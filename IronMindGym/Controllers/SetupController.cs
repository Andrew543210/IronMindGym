using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class SetupController(IronMindDbContext db) : Controller
{
    private const int DemoUserId = 1;

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().ToList(), "Id", "Name");
        return View(new SetupViewModel());
    }

    [HttpPost]
    public IActionResult Index(SetupViewModel model)
    {
        ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().ToList(), "Id", "Name", model.SplitTypeId);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = db.Users.Include(u => u.Benchmarks).First(u => u.Id == DemoUserId);
        user.Benchmarks ??= new InitialBenchmarks { UserId = user.Id };

        user.Benchmarks.BenchPressKg = model.BenchPressKg;
        user.Benchmarks.SquatKg = model.SquatKg;
        user.Benchmarks.DeadliftKg = model.DeadliftKg;

        db.AnthropometryLogs.Add(new Anthropometry
        {
            UserId = user.Id,
            WeightKg = model.WeightKg,
            HeightCm = model.HeightCm,
            RecordedAt = DateTime.UtcNow
        });

        var existingPlan = db.WorkoutPlans.FirstOrDefault(p => p.UserId == user.Id && p.IsActive);
        if (existingPlan is not null)
        {
            existingPlan.IsActive = false;
        }

        db.WorkoutPlans.Add(new WorkoutPlan
        {
            UserId = user.Id,
            WorkoutsPerWeek = model.WorkoutsPerWeek,
            SplitTypeId = model.SplitTypeId,
            IsActive = true
        });

        db.SaveChanges();
        TempData["Message"] = "Стартові дані збережено.";

        return RedirectToAction("Index", "Dashboard");
    }
}

