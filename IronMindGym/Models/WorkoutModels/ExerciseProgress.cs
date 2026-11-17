using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>
/// Tracks initial and current progress for a specific exercise by a user
/// </summary>
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

    // Initial Results (Strength Training)
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

    // Initial Results (Cardio)
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

    // Current Results (Strength Training)
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

    // Current Results (Cardio)
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

    // Validation Methods
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

    /// <summary>
    /// Calculate percentage progress for strength training
    /// Returns (CurrentWeight - InitialWeight) / InitialWeight * 100
    /// </summary>
    public double CalculateWeightProgressPercent()
    {
        if (InitialWeightKg <= 0)
            return 0;
        return ((CurrentWeightKg - InitialWeightKg) / InitialWeightKg) * 100;
    }

    /// <summary>
    /// Calculate absolute weight progress in kg
    /// </summary>
    public double CalculateWeightProgressKg() => CurrentWeightKg - InitialWeightKg;

    /// <summary>
    /// Calculate percentage progress for reps
    /// </summary>
    public double CalculateRepsProgressPercent()
    {
        if (InitialReps <= 0)
            return 0;
        return ((CurrentReps - InitialReps) / (double)InitialReps) * 100;
    }

    /// <summary>
    /// Calculate absolute reps progress
    /// </summary>
    public int CalculateRepsProgressCount() => CurrentReps - InitialReps;

    /// <summary>
    /// Calculate cardio duration progress in seconds
    /// </summary>
    public int CalculateCardioDurationProgress() => CurrentDurationSeconds - InitialDurationSeconds;

    /// <summary>
    /// Calculate cardio intensity progress
    /// </summary>
    public int CalculateCardioIntensityProgress() => CurrentIntensity - InitialIntensity;

    /// <summary>
    /// Calculate cardio effort ratio (current effort / initial effort)
    /// </summary>
    public double CalculateCardioEffortRatio()
    {
        int initialEffort = InitialDurationSeconds * InitialIntensity;
        int currentEffort = CurrentDurationSeconds * CurrentIntensity;
        if (initialEffort <= 0)
            return 0;
        return ((double)currentEffort / initialEffort - 1) * 100;
    }

    public override string ToString() =>
        Exercise?.Type == ExerciseType.Cardio
            ? $"Progress: {Exercise?.Name ?? "Unknown"} - {CalculateCardioEffortRatio():F1}% effort"
            : $"Progress: {Exercise?.Name ?? "Unknown"} - {CurrentWeightKg}kg vs Initial {InitialWeightKg}kg";
}


