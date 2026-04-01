using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MuscleSubGroup> SubGroups { get; set; } = new List<MuscleSubGroup>();
}

public class MuscleSubGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}

public class Exercise
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int MuscleSubGroupId { get; set; }
    public MuscleSubGroup MuscleSubGroup { get; set; } = null!;

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = new List<PerformedExercise>();
}

public class WorkoutSession
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; } = DateTime.UtcNow;
    public int DurationMinutes { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? SplitTypeId { get; set; }
    public SplitType? SplitType { get; set; }

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = new List<PerformedExercise>();
    public WorkoutReview? Review { get; set; }
}

public class PerformedExercise
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int OrderInWorkout { get; set; }

    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();
}

public class WorkoutSet
{
    public int Id { get; set; }
    public int PerformedExerciseId { get; set; }
    public PerformedExercise PerformedExercise { get; set; } = null!;

    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int SetNumber { get; set; }

    public int SetTypeId { get; set; }
    public SetType SetType { get; set; } = null!;
}

public class SetType
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();
}

public class WorkoutReview
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(800)]
    public string Comment { get; set; } = string.Empty;
}

public class PersonalRecord
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public double MaxWeightKg { get; set; }
}

public class HeartRateLog
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public int HeartRate { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

