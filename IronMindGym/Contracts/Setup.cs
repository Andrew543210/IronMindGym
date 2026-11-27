using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public record SetupRequest
{
    [Range(30, 250, ErrorMessage = "Enter a weight between 30 and 250 kg.")]
    public double WeightKg { get; init; }

    [Range(120, 230, ErrorMessage = "Enter a height between 120 and 230 cm.")]
    public double HeightCm { get; init; }

    [Range(0, 500, ErrorMessage = "The bench press must be between 0 and 500 kg.")]
    public double BenchPressKg { get; init; }

    [Range(0, 500, ErrorMessage = "The squat must be between 0 and 500 kg.")]
    public double SquatKg { get; init; }

    [Range(0, 500, ErrorMessage = "The deadlift must be between 0 and 500 kg.")]
    public double DeadliftKg { get; init; }

    [Range(1, 7, ErrorMessage = "Plan between 1 and 7 workouts a week.")]
    public int WorkoutsPerWeek { get; init; } = 3;

    public int SplitTypeId { get; init; }

    [MaxLength(64, ErrorMessage = "Time zone names are limited to 64 characters.")]
    public string? TimeZone { get; init; }
}

public sealed record SetupFormResponse(List<OptionResponse> SplitTypes, bool AlreadyCompleted);
