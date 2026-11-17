using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class ExerciseProgress
{
    public int Id
    {
        get; set;
    }

    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    public int ExerciseId
    {
        get; set;
    }
    public Exercise Exercise { get; set; } = null!;

    [Range(0, 500)]
    public double InitialWeightKg
    {
        get; set;
    }

    [Range(0, 500)]
    public int InitialReps
    {
        get; set;
    }

    [Range(0, 86400)]
    public int InitialDurationSeconds
    {
        get; set;
    }

    [Range(1, 10)]
    public int InitialIntensity
    {
        get; set;
    }

    [Range(0, 500)]
    public double CurrentWeightKg
    {
        get; set;
    }

    [Range(0, 500)]
    public int CurrentReps
    {
        get; set;
    }

    [Range(0, 86400)]
    public int CurrentDurationSeconds
    {
        get; set;
    }

    [Range(1, 10)]
    public int CurrentIntensity
    {
        get; set;
    }

    public DateTime InitialRecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime CurrentRecordedAt { get; set; } = DateTime.UtcNow;

    public bool IsValidStrengthInitial() => InitialWeightKg >= 0 && InitialReps > 0;
    public bool IsValidCardioInitial() => InitialDurationSeconds > 0 && InitialIntensity >= 1 && InitialIntensity <= 10;
    public bool IsValidStrengthCurrent() => CurrentWeightKg >= 0 && CurrentReps > 0;
    public bool IsValidCardioCurrent() => CurrentDurationSeconds > 0 && CurrentIntensity >= 1 && CurrentIntensity <= 10;

    public void SetInitialStrengthResults(double weight, int reps)
    {
        if (!double.IsFinite(weight) || weight < 0 || weight > 500)
            throw new ArgumentException("Weight must be between 0 and 500 kg.");
        if (reps <= 0 || reps > 500)
            throw new ArgumentException("Reps must be between 1 and 500.");

        InitialWeightKg = weight;
        InitialReps = reps;
        InitialRecordedAt = DateTime.UtcNow;
    }

    public void SetInitialCardioResults(int durationSeconds, int intensity)
    {
        if (durationSeconds <= 0 || durationSeconds > 86400)
            throw new ArgumentException("Duration must be between 1 and 86400 seconds.");
        if (intensity < 1 || intensity > 10)
            throw new ArgumentException("Intensity must be between 1 and 10.");

        InitialDurationSeconds = durationSeconds;
        InitialIntensity = intensity;
        InitialRecordedAt = DateTime.UtcNow;
    }

    public void SetCurrentStrengthResults(double weight, int reps)
    {
        if (!double.IsFinite(weight) || weight < 0 || weight > 500)
            throw new ArgumentException("Weight must be between 0 and 500 kg.");
        if (reps <= 0 || reps > 500)
            throw new ArgumentException("Reps must be between 1 and 500.");

        CurrentWeightKg = weight;
        CurrentReps = reps;
        CurrentRecordedAt = DateTime.UtcNow;
    }

    public void SetCurrentCardioResults(int durationSeconds, int intensity)
    {
        if (durationSeconds <= 0 || durationSeconds > 86400)
            throw new ArgumentException("Duration must be between 1 and 86400 seconds.");
        if (intensity < 1 || intensity > 10)
            throw new ArgumentException("Intensity must be between 1 and 10.");

        CurrentDurationSeconds = durationSeconds;
        CurrentIntensity = intensity;
        CurrentRecordedAt = DateTime.UtcNow;
    }

    public double CalculateWeightProgressPercent()
    {
        if (InitialWeightKg <= 0)
            return 0;
        return ((CurrentWeightKg - InitialWeightKg) / InitialWeightKg) * 100;
    }

    public double CalculateWeightProgressKg() => CurrentWeightKg - InitialWeightKg;

    public double CalculateRepsProgressPercent()
    {
        if (InitialReps <= 0)
            return 0;
        return ((CurrentReps - InitialReps) / (double)InitialReps) * 100;
    }

    public int CalculateRepsProgressCount() => CurrentReps - InitialReps;

    public int CalculateCardioDurationProgress() => CurrentDurationSeconds - InitialDurationSeconds;

    public int CalculateCardioIntensityProgress() => CurrentIntensity - InitialIntensity;

    public override string ToString() =>
        Exercise?.Type != ExerciseType.StrengthTraining
            ? $"Results: {Exercise?.Name ?? "Unknown"} - {CurrentDurationSeconds}s vs initial {InitialDurationSeconds}s"
            : $"Results: {Exercise?.Name ?? "Unknown"} - {CurrentWeightKg}kg × {CurrentReps} vs initial {InitialWeightKg}kg × {InitialReps}";
}
