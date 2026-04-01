using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ExerciseLibraryController(IronMindDbContext db) : Controller
{
    public IActionResult Index(int? muscleGroupId)
    {
        var groups = db.MuscleGroups.AsNoTracking().OrderBy(g => g.Name).ToList();

        var exercisesQuery = db.Exercises
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(sg => sg.MuscleGroup)
            .AsNoTracking()
            .AsQueryable();

        if (muscleGroupId.HasValue)
        {
            exercisesQuery = exercisesQuery.Where(e => e.MuscleSubGroup.MuscleGroupId == muscleGroupId.Value);
        }

        ViewBag.MuscleGroups = groups;
        ViewBag.SelectedGroupId = muscleGroupId;

        return View(exercisesQuery.OrderBy(e => e.Name).ToList());
    }
}

