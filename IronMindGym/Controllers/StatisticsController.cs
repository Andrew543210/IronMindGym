using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class StatisticsController(IronMindDbContext db, StatisticsService statistics) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(statistics.Build(User.GetUserId()!.Value));

    [HttpGet]
    public IActionResult Records()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var records = db.PersonalRecords
            .Where(pr => pr.UserId == currentUserId)
            .Include(pr => pr.Exercise)
            .ThenInclude(e => e.MuscleSubGroup)
            .ThenInclude(ms => ms.MuscleGroup)
            .AsNoTracking()
            .OrderBy(pr => pr.Exercise.MuscleSubGroup.MuscleGroup.Name)
            .ThenBy(pr => pr.Exercise.MuscleSubGroup.Name)
            .ThenBy(pr => pr.Exercise.Name)
            .Select(pr => new PersonalRecordListItemViewModel
            {
                MuscleGroupName = pr.Exercise.MuscleSubGroup.MuscleGroup.Name,
                MuscleSubGroupName = pr.Exercise.MuscleSubGroup.Name,
                ExerciseName = pr.Exercise.Name,
                MaxWeightKg = pr.MaxWeightKg,
                UpdatedAt = pr.UpdatedAt
            })
            .ToList();

        return View(records);
    }
}
