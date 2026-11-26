namespace IronMindGym.Models;

public class PerformedExercise
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int OrderInWorkout { get; set; }
    public ICollection<WorkoutSet> Sets { get; set; } = [];
}
