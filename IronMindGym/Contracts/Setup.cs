using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public record SetupRequest
{
    [Range(30, 250)]
    public double WeightKg { get; init; }

    [Range(120, 230)]
    public double HeightCm { get; init; }

    [Range(0, 500)]
    public double BenchPressKg { get; init; }

    [Range(0, 500)]
    public double SquatKg { get; init; }

    [Range(0, 500)]
    public double DeadliftKg { get; init; }

    [Range(1, 7)]
    public int WorkoutsPerWeek { get; init; } = 3;

    [Required]
    public int SplitTypeId { get; init; }

    [MaxLength(64)]
    public string? TimeZone { get; init; }
}

public sealed record SetupFormResponse(List<OptionResponse> SplitTypes, bool AlreadyCompleted);
