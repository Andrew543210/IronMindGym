namespace IronMindGym.Contracts;

/// <summary>Everything the athlete recorded, without credentials, for keeping a personal copy.</summary>
public sealed record DataExport(
    DateTime ExportedAtUtc,
    ExportedAccount Account,
    List<ExportedWeighIn> Weight,
    List<ExportedNutritionDay> Nutrition,
    List<ExportedWorkout> Workouts,
    List<ExportedProgress> Progress,
    List<ExportedGoal> Goals);

public sealed record ExportedAccount(string Username, string Email, int Age, double? HeightCm);

public sealed record ExportedWeighIn(DateOnly Date, double ValueKg);

public sealed record ExportedNutritionDay(DateOnly Date, double CaloriesKcal, double ProteinG, double FatG, double CarbsG);

public sealed record ExportedWorkout(string Name, DateTime SessionDate, int DurationMinutes, int? Rating, string? Notes, List<ExportedExercise> Exercises);

public sealed record ExportedExercise(string Name, List<ExportedSet> Sets);

public sealed record ExportedSet(int SetNumber, string SetType, double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds);

public sealed record ExportedProgress(
    string Name,
    double InitialWeightKg,
    double CurrentWeightKg,
    int InitialReps,
    int CurrentReps,
    int InitialDurationSeconds,
    int CurrentDurationSeconds,
    int InitialIntensity,
    int CurrentIntensity);

public sealed record ExportedGoal(string Title, DateOnly Deadline, bool IsCompleted);
