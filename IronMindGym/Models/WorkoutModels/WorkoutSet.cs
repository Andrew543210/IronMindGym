using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSet
{
    public int Id
    {
        get; set;
    }
    public int PerformedExerciseId
    {
        get; set;
    }
    public PerformedExercise PerformedExercise { get; set; } = null!;

    [Range(0, 1000)]
    public double WeightKg
    {
        get; set;
    }

    [Range(0, 500)]
    public int Reps
    {
        get; set;
    }

    [Range(0, 86400)]
    public int DurationSeconds
    {
        get; set;
    }

    [Range(1, 10)]
    public int Intensity { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int SetNumber
    {
        get; set;
    }

    [Range(0, 3600)]
    public int RestSeconds { get; set; } = 120;

    public int SetTypeId
    {
        get; set;
    }
    public SetType SetType { get; set; } = null!;

    public bool IsValidStrengthSet() => WeightKg >= 0 && Reps > 0;
    public bool IsValidCardioSet() => DurationSeconds > 0 && Intensity >= 1 && Intensity <= 10;
    public bool IsValidRest() => RestSeconds >= 0 && RestSeconds <= 3600;

    public void ValidateStrengthSet()
    {
        if (!IsValidStrengthSet())
            throw new ArgumentException("Strength set must have positive weight and reps.");
        if (!IsValidRest())
            throw new ArgumentException("Rest time must be between 0 and 3600 seconds.");
    }

    public void ValidateCardioSet()
    {
        if (!IsValidCardioSet())
            throw new ArgumentException("Cardio set must have duration > 0 and intensity 1-10.");
        if (!IsValidRest())
            throw new ArgumentException("Rest time must be between 0 and 3600 seconds.");
    }

    public double GetTonnage() => WeightKg * Reps;
    public int GetCardioEffort() => DurationSeconds * Intensity;

    public override string ToString() =>
        PerformedExercise?.Exercise?.Type == ExerciseType.Cardio
            ? $"Set #{SetNumber}: {DurationSeconds}s @ Intensity {Intensity}"
            : $"Set #{SetNumber}: {WeightKg}kg x {Reps}";
}
