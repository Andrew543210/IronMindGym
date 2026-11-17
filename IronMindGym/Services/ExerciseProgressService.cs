using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class ExerciseProgressService(IronMindDbContext db)
{
    public void SaveResults(int userId, SaveProgressViewModel model)
    {
        var type = db.Exercises.AsNoTracking().Where(e => e.Id == model.ExerciseId)
            .Select(e => (ExerciseType?)e.Type).FirstOrDefault()
            ?? throw new ArgumentException("Choose a valid exercise.");
        static int WholeNumber(double value)
        {
            if (!double.IsFinite(value) || value != Math.Truncate(value) || value < 0 || value > 86400)
                throw new ArgumentException("Repetitions, duration and intensity must be whole numbers within their allowed range.");
            return (int)value;
        }
        void SavePair(bool initial, double first, double second)
        {
            if (type == ExerciseType.StrengthTraining)
                Save(userId, model.ExerciseId, initial, first, WholeNumber(second), null, null);
            else if (type == ExerciseType.Cardio)
                Save(userId, model.ExerciseId, initial, null, null, WholeNumber(first), WholeNumber(second));
            else
                Save(userId, model.ExerciseId, initial, second, null, WholeNumber(first), null);
        }
        using var transaction = db.Database.BeginTransaction();
        SavePair(true, model.InitialValue1, model.InitialValue2);
        SavePair(false, model.CurrentValue1, model.CurrentValue2);
        transaction.Commit();
    }

    public List<ExerciseProgressEntry> List(int userId) => db.ExerciseProgress.AsNoTracking()
        .Include(p => p.Exercise).ThenInclude(e => e.MuscleSubGroup).ThenInclude(s => s.MuscleGroup)
        .Where(p => p.UserId == userId).OrderBy(p => p.Exercise.Name).ToList().Select(Map).ToList();

    public void Save(int userId, int exerciseId, bool initial, double? weight, int? reps, int? duration, int? intensity)
    {
        var exercise = db.Exercises.AsNoTracking().FirstOrDefault(e => e.Id == exerciseId)
            ?? throw new ArgumentException("Choose a valid exercise.");
        var progress = db.ExerciseProgress.FirstOrDefault(p => p.UserId == userId && p.ExerciseId == exerciseId);
        if (progress is null)
        {
            if (!initial)
                throw new ArgumentException("Save initial results first.");
            progress = new ExerciseProgress { UserId = userId, ExerciseId = exerciseId };
            db.ExerciseProgress.Add(progress);
        }
        if (exercise.Type == ExerciseType.Timed)
        {
            if (!double.IsFinite(weight ?? 0) || weight is < 0 or > 500 || duration is null or < 1 or > 86400)
                throw new ArgumentException("Timed exercises require a duration of 1–86400 seconds and 0–500 kg of external load.");
            if (initial)
            {
                progress.InitialWeightKg = weight ?? 0;
                progress.InitialDurationSeconds = duration.Value;
                progress.InitialIntensity = 1;
                progress.InitialRecordedAt = DateTime.UtcNow;
            }
            else
            {
                progress.CurrentWeightKg = weight ?? 0;
                progress.CurrentDurationSeconds = duration.Value;
                progress.CurrentIntensity = 1;
                progress.CurrentRecordedAt = DateTime.UtcNow;
            }
        }
        else if (exercise.Type == ExerciseType.Cardio)
        {
            if (initial)
                progress.SetInitialCardioResults(duration ?? 0, intensity ?? 0);
            else
                progress.SetCurrentCardioResults(duration ?? 0, intensity ?? 0);
        }
        else
        {
            if (initial)
                progress.SetInitialStrengthResults(weight ?? 0, reps ?? 0);
            else
                progress.SetCurrentStrengthResults(weight ?? 0, reps ?? 0);
        }
        db.SaveChanges();
    }

    public bool Delete(int userId, int id)
    {
        var progress = db.ExerciseProgress.FirstOrDefault(p => p.Id == id && p.UserId == userId);
        if (progress is null)
            return false;
        db.ExerciseProgress.Remove(progress);
        db.SaveChanges();
        return true;
    }

    private static ExerciseProgressEntry Map(ExerciseProgress progress)
    {
        return new ExerciseProgressEntry
        {
            Id = progress.Id,
            ExerciseProgressId = progress.Id,
            ExerciseId = progress.ExerciseId,
            ExerciseName = progress.Exercise.Name,
            MuscleSubGroup = progress.Exercise.MuscleSubGroup.Name,
            MuscleGroup = progress.Exercise.MuscleSubGroup.MuscleGroup.Name,
            ExerciseType = progress.Exercise.Type,

            InitialWeightKg = progress.InitialWeightKg,
            InitialReps = progress.InitialReps,
            InitialDurationSeconds = progress.InitialDurationSeconds,
            InitialIntensity = progress.InitialIntensity,
            InitialRecordedAt = progress.InitialRecordedAt,

            CurrentWeightKg = progress.CurrentWeightKg,
            CurrentReps = progress.CurrentReps,
            CurrentDurationSeconds = progress.CurrentDurationSeconds,
            CurrentIntensity = progress.CurrentIntensity,
            CurrentRecordedAt = progress.CurrentRecordedAt,

            WeightProgressKg = progress.CalculateWeightProgressKg(),
            WeightProgressPercent = progress.CalculateWeightProgressPercent(),
            RepsProgressCount = progress.CalculateRepsProgressCount(),
            RepsProgressPercent = progress.CalculateRepsProgressPercent(),
            CardioDurationProgressSeconds = progress.CalculateCardioDurationProgress(),
            CardioIntensityProgressCount = progress.CalculateCardioIntensityProgress()
        };


    }
}
