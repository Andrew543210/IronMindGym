using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
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
        db.Exercises.AsNoTracking()
            .Select(e => new ExerciseLibraryItem(
                e.Id,
                Localized.Pick(e.Name, e.NameUk),
                Localized.Pick(e.Description, e.DescriptionUk),
                e.Type,
                e.MuscleSubGroup.MuscleGroupId,
                Localized.Pick(e.MuscleSubGroup.Name, e.MuscleSubGroup.NameUk)))
            .AsEnumerable().OrderBy(e => e.Name, Localized.Order).ToList());
}

public sealed record ExerciseLibraryResponse(List<OptionResponse> MuscleGroups, List<ExerciseLibraryItem> Exercises);

public sealed record ExerciseLibraryItem(int Id, string Name, string Description, ExerciseType Type, int MuscleGroupId, string MuscleSubGroupName);
