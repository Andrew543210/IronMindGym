using IronMindGym.Models;
using System.ComponentModel.DataAnnotations;

namespace IronMindGym.ViewModels;

public class WorkoutLoggerViewModel
{
    [Required, MaxLength(120)]
    public string SessionName { get; set; } = "Силове тренування";

    [Range(10, 240)]
    public int DurationMinutes { get; set; } = 60;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public int? SplitTypeId { get; set; }

    [MaxLength(80)]
    public string SplitDayLabel { get; set; } = "День сили";

    [Required]
    public int ExerciseId { get; set; }

    [Range(0, 500)]
    public double WeightKg { get; set; } = 60;

    [Range(1, 50)]
    public int Reps { get; set; } = 8;

    [Required]
    public int SetTypeId { get; set; }

    public int RestSeconds { get; set; } = 120;

    public int? SelectedMuscleGroupId { get; set; }
    public int? SelectedMuscleSubGroupId { get; set; }

    public string DraftPayloadJson { get; set; } = "[]";

    [Range(1, 5)]
    public int ReviewRating { get; set; } = 4;

    [MaxLength(800)]
    public string ReviewComment { get; set; } = string.Empty;

    public List<ExerciseCatalogItemViewModel> ExerciseCatalog { get; set; } = new();

    public WorkoutSummaryViewModel? LastSummary { get; set; }

    public List<WorkoutSessionListItemViewModel> RecentSessions { get; set; } = new();
}

public class ExerciseCatalogItemViewModel
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int MuscleGroupId { get; set; }
    public string MuscleGroupName { get; set; } = string.Empty;
    public int MuscleSubGroupId { get; set; }
    public string MuscleSubGroupName { get; set; } = string.Empty;
}

public class DraftSetItemViewModel
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int SetTypeId { get; set; }
    public string SetTypeName { get; set; } = string.Empty;
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int RestSeconds { get; set; }
}

public class WorkoutSummaryViewModel
{
    public int WorkoutSessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public double AverageRestSeconds { get; set; }
    public int RemainingWorkoutsThisWeek { get; set; }
    public List<ExerciseWorkoutSummaryViewModel> ExerciseStats { get; set; } = new();
}

public class ExerciseWorkoutSummaryViewModel
{
    public string ExerciseName { get; set; } = string.Empty;
    public double AverageLoad { get; set; }
    public double PeakLoad { get; set; }
    public double PeakWeightKg { get; set; }
    public double? PercentToPersonalRecord { get; set; }
    public bool IsNewRecord { get; set; }
    public int SetsCount { get; set; }
}

public class WorkoutSessionListItemViewModel
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class WorkoutDetailsViewModel
{
    public WorkoutSession Session { get; set; } = null!;
    public WorkoutSummaryViewModel Summary { get; set; } = new();
}

