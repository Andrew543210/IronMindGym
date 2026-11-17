using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class SetupController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (HasCompletedSetup(userId.Value))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().ToList(), "Id", "Name");
        return View(new SetupViewModel());
    }

    [HttpPost]
    public IActionResult Index(SetupViewModel model)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (HasCompletedSetup(userId.Value))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().ToList(), "Id", "Name", model.SplitTypeId);

        if (!db.SplitTypes.Any(s => s.Id == model.SplitTypeId))
            ModelState.AddModelError(nameof(model.SplitTypeId), "Choose a valid training split.");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = userId.Value;
        var user = db.Users.Include(u => u.Benchmarks).First(u => u.Id == currentUserId);
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
        AchievementEngine.AssignAchievements(db, currentUserId);
        TempData["Message"] = "Initial setup data saved.";

        return RedirectToAction("Index", "Dashboard");
    }

    private bool HasCompletedSetup(int userId)
    {
        return db.Users.Any(u => u.Id == userId && u.Benchmarks != null)
               && db.AnthropometryLogs.Any(a => a.UserId == userId);
    }
}
