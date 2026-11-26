namespace IronMindGym.Models;

public class WorkoutSet
{
    public int Id { get; set; }
    public int PerformedExerciseId { get; set; }
    public PerformedExercise PerformedExercise { get; set; } = null!;
    public int SetTypeId { get; set; }
    public SetType SetType { get; set; } = null!;
    public int SetNumber { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int DurationSeconds { get; set; }
    public int Intensity { get; set; }
    public int RestSeconds { get; set; }
}
