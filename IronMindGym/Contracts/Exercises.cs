using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record ExerciseLibraryResponse(List<OptionResponse> MuscleGroups, List<ExerciseCatalogItem> Exercises);

/// <summary>An exercise of the catalog in the request language, with its English name for matching saved drafts.</summary>
public sealed record ExerciseCatalogItem
{
    public int ExerciseId { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public string EnglishName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }
    public int MuscleGroupId { get; init; }
    public string MuscleGroupName { get; init; } = string.Empty;
    public int MuscleSubGroupId { get; init; }
    public string MuscleSubGroupName { get; init; } = string.Empty;
}
