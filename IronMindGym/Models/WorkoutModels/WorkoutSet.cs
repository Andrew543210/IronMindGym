using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSet
{
    public int Id { get; set; }
    public int PerformedExerciseId { get; set; }
    public PerformedExercise PerformedExercise { get; set; } = null!;

    [Range(0, 1000)]
    public double WeightKg { get; set; }

    [Range(0, 500)]
    public int Reps { get; set; }

    [Range(1, int.MaxValue)]
    public int SetNumber { get; set; }

    [Range(0, 3600)]
    public int RestSeconds { get; set; } = 120;

    public int SetTypeId { get; set; }
    public SetType SetType { get; set; } = null!;

    public double GetTonnage() => WeightKg * Reps;

    public override string ToString() => $"Set #{SetNumber}: {WeightKg}kg x {Reps}";
}
