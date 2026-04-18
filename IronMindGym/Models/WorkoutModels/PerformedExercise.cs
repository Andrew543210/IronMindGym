using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class PerformedExercise
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int OrderInWorkout { get; set; }

    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();

    public void AddSet(WorkoutSet set)
    {
        Sets.Add(set ?? throw new ArgumentNullException(nameof(set)));
    }

    public override string ToString() => $"PerformedExercise: ExerciseId={ExerciseId}, Order={OrderInWorkout}";
}
