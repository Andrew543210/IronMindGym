using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class WorkoutLoggerController(IronMindDbContext db) : Controller
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
        var vm = new WorkoutLoggerViewModel
        {
            RecentSessions = db.WorkoutSessions
                .Where(s => s.UserId == currentUserId)
                .OrderByDescending(s => s.SessionDate)
                .Take(5)
                .ToList()
        };

        FillSelections(vm.ExerciseId, vm.SetTypeId);
        return View(vm);
    }

    [HttpPost]
    public IActionResult Index(WorkoutLoggerViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        FillSelections(model.ExerciseId, model.SetTypeId);

        if (!ModelState.IsValid)
        {
            model.RecentSessions = db.WorkoutSessions
                .Where(s => s.UserId == currentUserId)
                .OrderByDescending(s => s.SessionDate)
                .Take(5)
                .ToList();
            return View(model);
        }

        var session = new WorkoutSession
        {
            UserId = currentUserId,
            Name = model.SessionName,
            SessionDate = DateTime.UtcNow,
            DurationMinutes = model.DurationMinutes
        };

        var performedExercise = new PerformedExercise
        {
            ExerciseId = model.ExerciseId,
            OrderInWorkout = 1,
            WorkoutSession = session
        };

        for (var i = 1; i <= model.SetsCount; i++)
        {
            performedExercise.Sets.Add(new WorkoutSet
            {
                SetNumber = i,
                WeightKg = model.WeightKg,
                Reps = model.Reps,
                SetTypeId = model.SetTypeId
            });
        }

        db.PerformedExercises.Add(performedExercise);
        db.WorkoutReviews.Add(new WorkoutReview
        {
            WorkoutSession = session,
            Rating = 4,
            Comment = "Тренування завершено. Самопочуття нормальне."
        });

        db.SaveChanges();

        TempData["Message"] = "Тренування збережено.";
        return RedirectToAction(nameof(Index));
    }

    private void FillSelections(int selectedExerciseId, int selectedSetTypeId)
    {
        ViewBag.Exercises = new SelectList(db.Exercises.AsNoTracking().OrderBy(e => e.Name), "Id", "Name", selectedExerciseId);
        ViewBag.SetTypes = new SelectList(db.SetTypes.AsNoTracking().OrderBy(s => s.Name), "Id", "Name", selectedSetTypeId);
    }
}
