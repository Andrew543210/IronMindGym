using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public enum ExerciseType
{
    StrengthTraining = 0,
    Cardio = 1,
    Timed = 2
}

/// <summary>The three lifts behind the strength coefficient, the rank and the record awards.</summary>
public enum KeyLift
{
    BenchPress,
    Squat,
    Deadlift
}

public class Exercise
{
    public int Id { get; set; }

    /// <summary>The catalog key, unique in the database; <see cref="NameUk"/> is only shown.</summary>
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? NameUk { get; set; }

    [MaxLength(1000)]
    public string? DescriptionUk { get; set; }

    public ExerciseType Type { get; set; } = ExerciseType.StrengthTraining;

    /// <summary>Set for the one exercise that stands for each key lift.</summary>
    public KeyLift? KeyLift { get; set; }
    public int MuscleSubGroupId { get; set; }
    public MuscleSubGroup MuscleSubGroup { get; set; } = null!;
}
