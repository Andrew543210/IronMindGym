using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public class SaveWorkoutRequest
{
    [Range(1, 600)]
    public int DurationMinutes { get; set; } = 60;

    /// <summary>The split day trained; empty for a general training day.</summary>
    public int? SplitDayId { get; set; }

    public Guid? DraftId { get; set; }

    [Range(1, 5)]
    public int ReviewRating { get; set; } = 4;

    [MaxLength(800)]
    public string? ReviewComment { get; set; }

    public List<WorkoutSetInput> Sets { get; set; } = [];
}

public sealed record SplitDayOption(int Id, string Name);

public class ExerciseCatalogItem
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    public int MuscleGroupId { get; set; }
    public string MuscleGroupName { get; set; } = string.Empty;
    public int MuscleSubGroupId { get; set; }
    public string MuscleSubGroupName { get; set; } = string.Empty;
}

public class WorkoutSetInput
{
    public int ExerciseId { get; set; }
    public int SetTypeId { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int DurationSeconds { get; set; }
    public int Intensity { get; set; }
    public int RestSeconds { get; set; }
}

public class ExerciseWorkoutSummary
{
    public string ExerciseName { get; set; } = string.Empty;
    public int SetsCount { get; set; }
    public bool TracksLoad { get; set; }
    public double HeaviestLoadKg { get; set; }
    public double? PercentOfPreviousBest { get; set; }
    public bool IsNewRecord { get; set; }
}

public class WorkoutSessionListItem
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class WorkoutDetails
{
    public WorkoutSession Session { get; set; } = null!;
    public bool IsOwner { get; set; }
    public double AverageRestSeconds { get; set; }
    public int? RemainingWorkoutsThisWeek { get; set; }
    public List<ExerciseWorkoutSummary> ExerciseStats { get; set; } = [];
    public int LikesCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<WorkoutComment> Comments { get; set; } = [];
}

public class WorkoutComment
{
    public string AuthorUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
