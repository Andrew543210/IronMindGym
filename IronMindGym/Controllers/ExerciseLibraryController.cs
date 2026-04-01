using IronMindGym.Data;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ExerciseLibraryController(IronMindDbContext db) : Controller
{
    public IActionResult Index(int? muscleGroupId, int? muscleSubGroupId, string? search)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var groups = db.MuscleGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToList();

        var subGroupsQuery = db.MuscleSubGroups
            .AsNoTracking()
            .OrderBy(sg => sg.Name)
            .AsQueryable();

        if (muscleGroupId.HasValue)
        {
            subGroupsQuery = subGroupsQuery.Where(sg => sg.MuscleGroupId == muscleGroupId.Value);
        }

        var exercisesQuery = db.Exercises
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(sg => sg.MuscleGroup)
            .AsNoTracking()
            .AsQueryable();

        if (muscleGroupId.HasValue)
        {
            exercisesQuery = exercisesQuery.Where(e => e.MuscleSubGroup.MuscleGroupId == muscleGroupId.Value);
        }

        if (muscleSubGroupId.HasValue)
        {
            exercisesQuery = exercisesQuery.Where(e => e.MuscleSubGroupId == muscleSubGroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            exercisesQuery = exercisesQuery.Where(e =>
                e.Name.Contains(normalizedSearch) ||
                e.Description.Contains(normalizedSearch));
        }

        var vm = new ExerciseLibraryViewModel
        {
            MuscleGroups = groups,
            MuscleSubGroups = subGroupsQuery.ToList(),
            Exercises = exercisesQuery.OrderBy(e => e.Name).ToList(),
            SelectedGroupId = muscleGroupId,
            SelectedSubGroupId = muscleSubGroupId,
            Search = search ?? string.Empty
        };

        return View(vm);
    }
}
