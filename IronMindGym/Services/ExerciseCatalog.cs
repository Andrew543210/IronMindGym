using IronMindGym.Contracts;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public static class ExerciseCatalog
{
    public static List<ExerciseCatalogItem> Items(IQueryable<Exercise> exercises) => exercises.AsNoTracking()
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
        // Sorted here rather than in SQL: the database collation does not know the request language.
        .AsEnumerable()
        .OrderBy(e => e.ExerciseName, Localized.Order)
        .ToList();
}
