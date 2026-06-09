using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>
/// Keeps a starting and a latest result per exercise. What the two numbers of a result mean depends on the
/// exercise: weight and repetitions for strength, duration and effort for cardio, duration and load for timed holds.
/// </summary>
public class ExerciseProgressService(IronMindDbContext db)
{
    public List<ExerciseProgressEntry> List(int userId) => db.ExerciseProgress.AsNoTracking()
        .Where(p => p.UserId == userId)
        .Select(p => new ExerciseProgressEntry
        {
            ExerciseId = p.ExerciseId,
            ExerciseName = Localized.Pick(p.Exercise.Name, p.Exercise.NameUk),
            MuscleSubGroup = Localized.Pick(p.Exercise.MuscleSubGroup.Name, p.Exercise.MuscleSubGroup.NameUk),
            MuscleGroup = Localized.Pick(p.Exercise.MuscleSubGroup.MuscleGroup.Name, p.Exercise.MuscleSubGroup.MuscleGroup.NameUk),
            ExerciseType = p.Exercise.Type,
            InitialWeightKg = p.InitialWeightKg,
            InitialReps = p.InitialReps,
            InitialDurationSeconds = p.InitialDurationSeconds,
            InitialIntensity = p.InitialIntensity,
            CurrentWeightKg = p.CurrentWeightKg,
            CurrentReps = p.CurrentReps,
            CurrentDurationSeconds = p.CurrentDurationSeconds,
            CurrentIntensity = p.CurrentIntensity
        })
        .AsEnumerable()
        .OrderBy(p => p.ExerciseName, Localized.Order)
        .ToList();

    public ProgressPair? Results(int userId, int exerciseId)
    {
        var saved = db.ExerciseProgress.AsNoTracking().Include(p => p.Exercise)
            .FirstOrDefault(p => p.UserId == userId && p.ExerciseId == exerciseId);
        return saved?.Exercise.Type switch
        {
            null => null,
            ExerciseType.StrengthTraining => new(exerciseId, saved.InitialWeightKg, saved.InitialReps, saved.CurrentWeightKg, saved.CurrentReps),
            ExerciseType.Cardio => new(exerciseId, saved.InitialDurationSeconds, saved.InitialIntensity, saved.CurrentDurationSeconds, saved.CurrentIntensity),
            _ => new(exerciseId, saved.InitialDurationSeconds, saved.InitialWeightKg, saved.CurrentDurationSeconds, saved.CurrentWeightKg)
        };
    }

    /// <summary>Saves both results together: when either is invalid, neither changes.</summary>
    public void SaveResults(int userId, ProgressPair pair)
    {
        var exercise = FindExercise(pair.ExerciseId);
        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForAthletes(db, userId);
        var progress = db.ExerciseProgress.FirstOrDefault(p => p.UserId == userId && p.ExerciseId == exercise.Id);
        if (progress is null)
            db.ExerciseProgress.Add(progress = new ExerciseProgress { UserId = userId, ExerciseId = exercise.Id });
        Record(progress, initial: true, Result.FromPair(exercise.Type, pair.InitialValue1, pair.InitialValue2));
        Record(progress, initial: false, Result.FromPair(exercise.Type, pair.CurrentValue1, pair.CurrentValue2));
        db.SaveChanges();
        transaction.Commit();
    }

    public void SaveCurrent(int userId, int exerciseId, double? weight, int? reps, int? duration, int? intensity)
    {
        var exercise = FindExercise(exerciseId);
        var result = Result.Validate(exercise.Type, weight ?? 0, reps ?? 0, duration ?? 0, intensity ?? 0);
        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForAthletes(db, userId);
        var progress = db.ExerciseProgress.FirstOrDefault(p => p.UserId == userId && p.ExerciseId == exerciseId)
            ?? throw new ArgumentException("Save initial results first.");
        Record(progress, initial: false, result);
        db.SaveChanges();
        transaction.Commit();
    }

    public bool Delete(int userId, int exerciseId) =>
        db.ExerciseProgress.Where(p => p.UserId == userId && p.ExerciseId == exerciseId).ExecuteDelete() > 0;

    private Exercise FindExercise(int exerciseId) =>
        db.Exercises.AsNoTracking().FirstOrDefault(e => e.Id == exerciseId) ?? throw new ArgumentException("Choose a valid exercise.");

    private static void Record(ExerciseProgress progress, bool initial, Result result)
    {
        if (initial)
        {
            progress.InitialWeightKg = result.WeightKg;
            progress.InitialReps = result.Reps;
            progress.InitialDurationSeconds = result.DurationSeconds;
            progress.InitialIntensity = result.Intensity;
            progress.InitialRecordedAt = DateTime.UtcNow;
        }
        else
        {
            progress.CurrentWeightKg = result.WeightKg;
            progress.CurrentReps = result.Reps;
            progress.CurrentDurationSeconds = result.DurationSeconds;
            progress.CurrentIntensity = result.Intensity;
            progress.CurrentRecordedAt = DateTime.UtcNow;
        }
    }

    private readonly record struct Result(double WeightKg, int Reps, int DurationSeconds, int Intensity)
    {
        public static Result FromPair(ExerciseType type, double first, double second) => type switch
        {
            ExerciseType.StrengthTraining => Validate(type, first, WholeNumber(second), 0, 0),
            ExerciseType.Cardio => Validate(type, 0, 0, WholeNumber(first), WholeNumber(second)),
            _ => Validate(type, second, 0, WholeNumber(first), 0)
        };

        public static Result Validate(ExerciseType type, double weight, int reps, int duration, int intensity)
        {
            var validLoad = double.IsFinite(weight) && weight is >= 0 and <= 500;
            switch (type)
            {
                case ExerciseType.StrengthTraining:
                    if (!validLoad)
                        throw new ArgumentException("Weight must be between 0 and 500 kg.");
                    if (reps is < 1 or > 500)
                        throw new ArgumentException("Reps must be between 1 and 500.");
                    return new(weight, reps, 0, 0);
                case ExerciseType.Cardio:
                    if (duration is < 1 or > 86400)
                        throw new ArgumentException("Duration must be between 1 and 86400 seconds.");
                    if (intensity is < 1 or > 10)
                        throw new ArgumentException("Intensity must be between 1 and 10.");
                    return new(0, 0, duration, intensity);
                default:
                    if (!validLoad || duration is < 1 or > 86400)
                        throw new ArgumentException("Timed exercises require a duration of 1–86400 seconds and 0–500 kg of external load.");
                    return new(weight, 0, duration, 0);
            }
        }

        private static int WholeNumber(double value)
        {
            if (!double.IsFinite(value) || value != Math.Truncate(value) || value < 0 || value > 86400)
                throw new ArgumentException("Repetitions, duration and intensity must be whole numbers within their allowed range.");
            return (int)value;
        }
    }
}
