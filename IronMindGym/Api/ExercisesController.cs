using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

/// <summary>The whole catalog in the request language. It is small, so the client filters and searches it locally.</summary>
[Route("api/exercises")]
public class ExercisesController(IronMindDbContext db) : ApiController
{
    [HttpGet]
    public ExerciseLibraryResponse Library() => new(
        db.MuscleGroups.AsNoTracking()
            .Select(g => new OptionResponse(g.Id, Localized.Pick(g.Name, g.NameUk)))
            .AsEnumerable().OrderBy(g => g.Name, Localized.Order).ToList(),
        ExerciseCatalog.Items(db.Exercises));
}
