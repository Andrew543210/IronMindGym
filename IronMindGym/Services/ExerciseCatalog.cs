using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

// Names are sorted here rather than in SQL: the database collation does not know the request language.
public sealed class ExerciseCatalog(IronMindDbContext db)
{
    public async Task<List<ExerciseCatalogItem>> ItemsAsync(CancellationToken cancellationToken)
    {
        var items = await db.Exercises.AsNoTracking()
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
            .ToListAsync(cancellationToken);
        return items.OrderBy(e => e.ExerciseName, Localized.Order).ToList();
    }

    public async Task<List<OptionResponse>> MuscleGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await db.MuscleGroups.AsNoTracking()
            .Select(g => new OptionResponse(g.Id, Localized.Pick(g.Name, g.NameUk)))
            .ToListAsync(cancellationToken);
        return groups.OrderBy(g => g.Name, Localized.Order).ToList();
    }

    public async Task<ExerciseLibraryResponse> LibraryAsync(CancellationToken cancellationToken) =>
        new(await MuscleGroupsAsync(cancellationToken), await ItemsAsync(cancellationToken));
}
