using IronMindGym.Models;

namespace IronMindGym.Services;

/// <summary>The values a result keeps for its exercise type; the ones the type does not record are zero.</summary>
public readonly record struct TrackedResult(double WeightKg, int Reps, int DurationSeconds, int Intensity);

/// <summary>
/// What a result of each exercise type records and the allowed range of each value. The workout journal and the
/// progress page both check results here, so a value is refused with the same message wherever it is entered.
/// The database holds the same limits as check constraints.
/// </summary>
public static class TrackingRules
{
    public const double MaxLoadKg = 500;
    public const int MaxReps = 500;
    public const int MaxDurationSeconds = 86_400;
    public const int MaxIntensity = 10;
    public const int MaxRestSeconds = 3600;

    /// <summary>
    /// Strength results record load and reps, cardio records duration and effort, and timed exercises
    /// record duration and an optional external load.
    /// </summary>
    public static TrackedResult Check(ExerciseType type, double weightKg, int reps, int durationSeconds, int intensity)
    {
        switch (type)
        {
            case ExerciseType.StrengthTraining:
                CheckLoad(weightKg);
                if (reps is < 1 or > MaxReps)
                    throw new DomainValidationException($"Reps must be between 1 and {MaxReps}.");
                return new(weightKg, reps, 0, 0);
            case ExerciseType.Cardio:
                CheckDuration(durationSeconds);
                if (intensity is < 1 or > MaxIntensity)
                    throw new DomainValidationException($"Intensity must be between 1 and {MaxIntensity}.");
                return new(0, 0, durationSeconds, intensity);
            default:
                CheckDuration(durationSeconds);
                CheckLoad(weightKg);
                return new(weightKg, 0, durationSeconds, 0);
        }
    }

    public static void CheckRest(int restSeconds)
    {
        if (restSeconds is < 0 or > MaxRestSeconds)
            throw new DomainValidationException($"Rest must be between 0 and {MaxRestSeconds} seconds.");
    }

    private static void CheckLoad(double weightKg)
    {
        if (!double.IsFinite(weightKg) || weightKg is < 0 or > MaxLoadKg)
            throw new DomainValidationException($"Weight must be between 0 and {MaxLoadKg} kg.");
    }

    private static void CheckDuration(int durationSeconds)
    {
        if (durationSeconds is < 1 or > MaxDurationSeconds)
            throw new DomainValidationException($"Duration must be between 1 and {MaxDurationSeconds} seconds.");
    }
}
