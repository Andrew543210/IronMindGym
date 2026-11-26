using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record SaveWorkoutRequest
{
    [Range(1, 600)]
    public int DurationMinutes { get; init; } = 60;

    /// <summary>The split day trained; empty for a general training day.</summary>
    public int? SplitDayId { get; init; }

    public Guid? DraftId { get; init; }

    [Range(1, 5)]
    public int ReviewRating { get; init; } = 4;

    [MaxLength(800)]
    public string? ReviewComment { get; init; }

    public List<WorkoutSetInput> Sets { get; init; } = [];
}

public sealed record SplitDayOption(int Id, string Name);

public sealed record WorkoutSetInput
{
    public int ExerciseId { get; init; }
    public int SetTypeId { get; init; }
    public double WeightKg { get; init; }
    public int Reps { get; init; }
    public int DurationSeconds { get; init; }
    public int Intensity { get; init; }
    public int RestSeconds { get; init; }
}

public sealed record ExerciseWorkoutSummary
{
    public string ExerciseName { get; init; } = string.Empty;
    public int SetsCount { get; init; }
    public bool TracksLoad { get; init; }
    public double HeaviestLoadKg { get; init; }
    public double? PercentOfPreviousBest { get; init; }
    public bool IsNewRecord { get; init; }
}

public sealed record WorkoutSessionListItem
{
    public int Id { get; init; }
    public DateTime SessionDate { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
}

public sealed record WorkoutComment
{
    public string AuthorUsername { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record WorkoutDraftRequest([Required] Guid? DraftId, [Required] JsonElement? Content);

public sealed record WorkoutDraftResponse(Guid DraftId, JsonElement Content, DateTime UpdatedAt);

public sealed record WorkoutHistoryResponse(List<WorkoutSessionListItem> Sessions, int Page, bool HasOlderSessions);

public sealed record SplitTypeOption(int Id, string Name, List<SplitDayOption> Days);

public sealed record WorkoutLoggerResponse(
    string? ActiveSplitTypeName,
    int? ActiveSplitTypeId,
    List<SplitTypeOption> SplitTypes,
    List<OptionResponse> SetTypes,
    int DefaultSetTypeId,
    List<OptionResponse> MuscleGroups,
    List<ExerciseCatalogItem> Exercises);

public sealed record PreviousSetResponse(double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds, int SetTypeId, DateOnly LocalDate);

public sealed record SavedWorkoutResponse(int Id);

public sealed record CommentRequest(string? Text);

public sealed record WorkoutSetResponse(int SetNumber, string? SetTypeName, double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds);

public sealed record PerformedExerciseResponse(string ExerciseName, ExerciseType ExerciseType, List<WorkoutSetResponse> Sets);

public sealed record WorkoutDetailsResponse(
    int Id,
    string Name,
    DateTime SessionDate,
    int DurationMinutes,
    int? Rating,
    string? Notes,
    List<PerformedExerciseResponse> Exercises,
    double AverageRestSeconds,
    int? RemainingWorkoutsThisWeek,
    List<ExerciseWorkoutSummary> ExerciseStats,
    bool IsOwner,
    int LikesCount,
    bool IsLikedByCurrentUser,
    List<WorkoutComment> Comments);
