using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record ProgressPair(int ExerciseId, double InitialValue1, double InitialValue2, double CurrentValue1, double CurrentValue2);

public class ExerciseProgressEntry
{
    public int ExerciseId { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public string MuscleGroup { get; init; } = string.Empty;
    public string MuscleSubGroup { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }

    public double InitialWeightKg { get; init; }
    public int InitialReps { get; init; }
    public int InitialDurationSeconds { get; init; }
    public int InitialIntensity { get; init; }

    public double CurrentWeightKg { get; init; }
    public int CurrentReps { get; init; }
    public int CurrentDurationSeconds { get; init; }
    public int CurrentIntensity { get; init; }

    public bool HasCurrentResult => ExerciseType == ExerciseType.StrengthTraining ? CurrentReps > 0 : CurrentDurationSeconds > 0;
    public double WeightProgressKg => CurrentWeightKg - InitialWeightKg;
    public int RepsProgressCount => CurrentReps - InitialReps;
    public int CardioDurationProgressSeconds => CurrentDurationSeconds - InitialDurationSeconds;
    public int CardioIntensityProgressCount => CurrentIntensity - InitialIntensity;
}
