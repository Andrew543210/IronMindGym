using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>A starting result and the latest result of one exercise, compared on the progress tracker.</summary>
public class ExerciseProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    [Range(0, 500)]
    public double InitialWeightKg { get; set; }

    [Range(0, 500)]
    public int InitialReps { get; set; }

    [Range(0, 86400)]
    public int InitialDurationSeconds { get; set; }

    [Range(0, 10)]
    public int InitialIntensity { get; set; }

    [Range(0, 500)]
    public double CurrentWeightKg { get; set; }

    [Range(0, 500)]
    public int CurrentReps { get; set; }

    [Range(0, 86400)]
    public int CurrentDurationSeconds { get; set; }

    [Range(0, 10)]
    public int CurrentIntensity { get; set; }

    public DateTime InitialRecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime CurrentRecordedAt { get; set; } = DateTime.UtcNow;

    public double WeightChangePercent => InitialWeightKg > 0 ? (CurrentWeightKg - InitialWeightKg) / InitialWeightKg * 100 : 0;
}
