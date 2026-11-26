namespace IronMindGym.Models;

/// <summary>A starting result and the latest result of one exercise, compared on the progress tracker.</summary>
public class ExerciseProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public double InitialWeightKg { get; set; }
    public int InitialReps { get; set; }
    public int InitialDurationSeconds { get; set; }
    public int InitialIntensity { get; set; }
    public double CurrentWeightKg { get; set; }
    public int CurrentReps { get; set; }
    public int CurrentDurationSeconds { get; set; }
    public int CurrentIntensity { get; set; }
    public DateTime InitialRecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime CurrentRecordedAt { get; set; } = DateTime.UtcNow;

    public double WeightChangePercent => InitialWeightKg > 0 ? (CurrentWeightKg - InitialWeightKg) / InitialWeightKg * 100 : 0;
}
