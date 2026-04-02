using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSet
{
    public int Id { get; set; }
    public int PerformedExerciseId { get; set; }
    public PerformedExercise PerformedExercise { get; set; } = null!;
    public int SetTypeId { get; set; }
    public SetType SetType { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int SetNumber { get; set; }

    [Range(0, 500)]
    public double WeightKg { get; set; }

    [Range(0, 500)]
    public int Reps { get; set; }

    [Range(0, 86400)]
    public int DurationSeconds { get; set; }

    [Range(0, 10)]
    public int Intensity { get; set; }

    [Range(0, 3600)]
    public int RestSeconds { get; set; }
}
