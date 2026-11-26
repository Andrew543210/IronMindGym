using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>
/// The whole exercise catalog in the request language. It is small, so the client filters and searches it locally.
/// Names are sorted here rather than in SQL: the database collation does not know the request language.
/// </summary>
public sealed class ExerciseCatalog(IronMindDbContext db)
{
    public List<ExerciseCatalogItem> Items() => db.Exercises.AsNoTracking()
        .Select(e => new ExerciseCatalogItem
        {
            ExerciseId = e.Id,
            ExerciseName = Localized.Pick(e.Name, e.NameUk),
            EnglishName = e.Name,
            Description = Localized.Pick(e.Description, e.DescriptionUk),
            ExerciseType = e.Type,
            MuscleGroupId = e.MuscleSubGroup.MuscleGroupId,
            MuscleGroupName = Localized.Pick(e.MuscleSubGroup.MuscleGroup.Name, e.MuscleSubGroup.MuscleGroup.NameUk),
            MuscleSubGroupId = e.MuscleSubGroupId,
            MuscleSubGroupName = Localized.Pick(e.MuscleSubGroup.Name, e.MuscleSubGroup.NameUk)
        })
        .AsEnumerable()
        .OrderBy(e => e.ExerciseName, Localized.Order)
        .ToList();

    public List<OptionResponse> MuscleGroups() => db.MuscleGroups.AsNoTracking()
        .Select(g => new OptionResponse(g.Id, Localized.Pick(g.Name, g.NameUk)))
        .AsEnumerable().OrderBy(g => g.Name, Localized.Order).ToList();

    public ExerciseLibraryResponse Library() => new(MuscleGroups(), Items());
}
