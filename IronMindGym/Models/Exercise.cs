using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public enum ExerciseType
{
    StrengthTraining = 0,
    Cardio = 1,
    Timed = 2
}

public enum KeyLift
{
    BenchPress,
    Squat,
    Deadlift
}

public class Exercise
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? NameUk { get; set; }

    [MaxLength(1000)]
    public string? DescriptionUk { get; set; }

    public ExerciseType Type { get; set; } = ExerciseType.StrengthTraining;

    public KeyLift? KeyLift { get; set; }
    public int MuscleSubGroupId { get; set; }
    public MuscleSubGroup MuscleSubGroup { get; set; } = null!;
}
