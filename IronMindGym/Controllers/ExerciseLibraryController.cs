using IronMindGym.Data;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ExerciseLibraryController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index(int? muscleGroupId, int? muscleSubGroupId, string? search)
    {
        var subgroups = db.MuscleSubGroups.AsNoTracking().AsQueryable();
        var exercises = db.Exercises.AsNoTracking().Include(e => e.MuscleSubGroup).ThenInclude(s => s.MuscleGroup).AsQueryable();
        if (muscleGroupId.HasValue)
        {
            subgroups = subgroups.Where(s => s.MuscleGroupId == muscleGroupId);
            exercises = exercises.Where(e => e.MuscleSubGroup.MuscleGroupId == muscleGroupId);
        }
        if (muscleSubGroupId.HasValue)
            exercises = exercises.Where(e => e.MuscleSubGroupId == muscleSubGroupId);
        var term = (search ?? "").Trim();
        if (term.Length > 0)
            exercises = exercises.Where(e => e.Name.ToLower().Contains(term.ToLower()) || e.Description.ToLower().Contains(term.ToLower()));
        return View(new ExerciseLibraryViewModel
        {
            MuscleGroups = db.MuscleGroups.AsNoTracking().OrderBy(g => g.Name).ToList(),
            MuscleSubGroups = subgroups.OrderBy(s => s.Name).ToList(),
            Exercises = exercises.OrderBy(e => e.Name).ToList(),
            SelectedGroupId = muscleGroupId,
            SelectedSubGroupId = muscleSubGroupId,
            Search = term
        });
    }
}
