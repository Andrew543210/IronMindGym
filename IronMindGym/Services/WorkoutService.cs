using System.ComponentModel.DataAnnotations;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutService(IronMindDbContext db)
{
    public WorkoutDetails? BuildDetails(int ownerId, int sessionId, int viewerId)
    {
        var session = db.WorkoutSessions.AsNoTracking()
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Sets).ThenInclude(set => set.SetType)
            .Include(s => s.Review)
            .FirstOrDefault(s => s.Id == sessionId && s.UserId == ownerId);
        if (session is null)
            return null;
        var sets = session.PerformedExercises.SelectMany(pe => pe.Sets).ToList();
        return new WorkoutDetails
        {
            Session = session,
            IsOwner = viewerId == ownerId,
            LikesCount = db.ActivityLikes.Count(x => x.WorkoutSessionId == sessionId),
            IsLikedByCurrentUser = db.ActivityLikes.Any(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId),
            Comments = db.FriendComments.AsNoTracking()
                .Where(x => x.WorkoutSessionId == sessionId)
                .OrderByDescending(x => x.Date)
                .Select(x => new WorkoutComment { AuthorUsername = x.Author.Username, Text = x.Text, Date = x.Date })
                .ToList(),
            AverageRestSeconds = sets.Count > 0 ? Math.Round(sets.Average(s => s.RestSeconds), 1) : 0,
            ExerciseStats = BuildExerciseStats(session)
        };
    }

    /// <summary>
    /// Heaviest external load per exercise compared with the best load logged before this workout.
    /// Later workouts never change the comparison. Only strength exercises have load records.
    /// </summary>
    public List<ExerciseWorkoutSummary> BuildExerciseStats(WorkoutSession session)
    {
        var exerciseIds = session.PerformedExercises.Select(p => p.ExerciseId).Distinct().ToList();
        var previousRecords = db.Sets.AsNoTracking()
            .Where(s => s.PerformedExercise.WorkoutSession.UserId == session.UserId
                && exerciseIds.Contains(s.PerformedExercise.ExerciseId)
                && s.PerformedExercise.Exercise.Type == ExerciseType.StrengthTraining
                && (s.PerformedExercise.WorkoutSession.SessionDate < session.SessionDate
                    || (s.PerformedExercise.WorkoutSession.SessionDate == session.SessionDate
                        && s.PerformedExercise.WorkoutSessionId < session.Id)))
            .GroupBy(s => s.PerformedExercise.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Weight = g.Max(s => s.WeightKg) })
            .ToDictionary(x => x.ExerciseId, x => x.Weight);
        return session.PerformedExercises.OrderBy(p => p.OrderInWorkout).GroupBy(p => p.ExerciseId)
            .Select(group =>
            {
                var exercise = group.First().Exercise;
                var sets = group.SelectMany(p => p.Sets).ToList();
                var heaviest = sets.Max(s => s.WeightKg);
                var strength = exercise.Type == ExerciseType.StrengthTraining;
                previousRecords.TryGetValue(group.Key, out var previous);
                return new ExerciseWorkoutSummary
                {
                    ExerciseName = Localized.Pick(exercise.Name, exercise.NameUk),
                    SetsCount = sets.Count,
                    TracksLoad = strength,
                    HeaviestLoadKg = heaviest,
                    PercentOfPreviousBest = strength && previous > 0 ? Math.Round(heaviest / previous * 100, 1) : null,
                    IsNewRecord = strength && heaviest > 0 && heaviest > previous
                };
            }).ToList();
    }

    /// <summary>
    /// Saves a finished workout, updates personal records and awards.
    /// A retry with the same draft id returns the workout saved by the first request.
    /// </summary>
    public int Save(int currentUserId, SaveWorkoutRequest workout)
    {
        Validator.ValidateObject(workout, new ValidationContext(workout), validateAllProperties: true);
        if (workout.Sets is null || workout.Sets.Count is < 1 or > 200 || workout.Sets.Any(s => s is null))
            throw new ValidationException("Add between 1 and 200 sets before saving.");
        var exercises = ValidateSets(workout.Sets);
        if (workout.SplitTypeId.HasValue && !db.SplitTypes.Any(s => s.Id == workout.SplitTypeId))
            throw new ValidationException("Choose a valid training split.");

        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForAthletes(db, currentUserId);
        if (workout.DraftId.HasValue)
        {
            db.WorkoutDrafts.Where(d => d.UserId == currentUserId && d.DraftId == workout.DraftId).ExecuteDelete();
            var savedId = db.WorkoutSessions.Where(s => s.UserId == currentUserId && s.DraftId == workout.DraftId)
                .Select(s => (int?)s.Id).FirstOrDefault();
            if (savedId.HasValue)
                return savedId.Value;
        }
        var splitDayLabel = workout.SplitDayLabel.Trim();
        var sessionName = splitDayLabel.EndsWith("day", StringComparison.OrdinalIgnoreCase)
            ? splitDayLabel
            : $"{splitDayLabel} day";

        var session = new WorkoutSession
        {
            UserId = currentUserId,
            Name = sessionName,
            SessionDate = DateTime.UtcNow,
            DraftId = workout.DraftId,
            DurationMinutes = workout.DurationMinutes,
            SplitTypeId = workout.SplitTypeId
        };

        // Keep contiguous blocks so supersets and circuits retain their logged order.
        PerformedExercise? performedExercise = null;
        var setNumbers = new Dictionary<int, int>();
        foreach (var set in workout.Sets)
        {
            if (performedExercise?.ExerciseId != set.ExerciseId)
            {
                performedExercise = new PerformedExercise
                {
                    ExerciseId = set.ExerciseId,
                    WorkoutSession = session,
                    OrderInWorkout = session.PerformedExercises.Count + 1
                };
                session.PerformedExercises.Add(performedExercise);
            }
            setNumbers.TryGetValue(set.ExerciseId, out var setNumber);
            setNumbers[set.ExerciseId] = ++setNumber;
            performedExercise.Sets.Add(new WorkoutSet
            {
                SetNumber = setNumber,
                WeightKg = set.WeightKg,
                Reps = set.Reps,
                SetTypeId = set.SetTypeId,
                DurationSeconds = set.DurationSeconds,
                Intensity = set.Intensity,
                RestSeconds = set.RestSeconds
            });
        }
        db.WorkoutSessions.Add(session);
        foreach (var exerciseGroup in workout.Sets.GroupBy(x => x.ExerciseId))
        {
            var peakWeight = exerciseGroup.Max(x => x.WeightKg);
            var existingRecord = db.PersonalRecords.FirstOrDefault(pr => pr.UserId == currentUserId && pr.ExerciseId == exerciseGroup.Key);

            var isStrength = exercises[exerciseGroup.Key].Type == ExerciseType.StrengthTraining;
            var isNewRecord = isStrength && peakWeight > 0 && (existingRecord == null || peakWeight > existingRecord.MaxWeightKg);
            if (isNewRecord)
            {
                if (existingRecord == null)
                {
                    db.PersonalRecords.Add(new PersonalRecord
                    {
                        UserId = currentUserId,
                        ExerciseId = exerciseGroup.Key,
                        MaxWeightKg = peakWeight,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingRecord.MaxWeightKg = peakWeight;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        db.WorkoutReviews.Add(new WorkoutReview
        {
            WorkoutSession = session,
            Rating = workout.ReviewRating,
            Comment = workout.ReviewComment?.Trim() ?? string.Empty
        });

        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, currentUserId);
        transaction.Commit();
        return session.Id;
    }

    private Dictionary<int, Exercise> ValidateSets(List<WorkoutSetInput> sets)
    {
        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList();
        var exercises = db.Exercises.AsNoTracking().Where(e => exerciseIds.Contains(e.Id)).ToDictionary(e => e.Id);
        var setTypeIds = db.SetTypes.Select(s => s.Id).ToHashSet();
        foreach (var set in sets)
        {
            if (!exercises.TryGetValue(set.ExerciseId, out var exercise) || !setTypeIds.Contains(set.SetTypeId))
                throw new ValidationException("Choose valid exercises and set types.");
            if (set.RestSeconds is < 0 or > 3600)
                throw new ValidationException("Rest must be between 0 and 3600 seconds.");
            NormalizeForTrackingMode(set, exercise.Type);
        }
        return exercises;
    }

    private static void NormalizeForTrackingMode(WorkoutSetInput set, ExerciseType type)
    {
        var validLoad = double.IsFinite(set.WeightKg) && set.WeightKg is >= 0 and <= 500;
        if (type == ExerciseType.StrengthTraining)
        {
            if (!validLoad || set.Reps is < 1 or > 500)
                throw new ValidationException("Strength sets require 0–500 kg and 1–500 reps.");
            set.DurationSeconds = 0;
            set.Intensity = 0;
            return;
        }
        if (set.DurationSeconds is < 1 or > 86400)
            throw new ValidationException("Duration must be between 1 and 86400 seconds.");
        if (type == ExerciseType.Cardio)
        {
            if (set.Intensity is < 1 or > 10)
                throw new ValidationException("Cardio effort must be between 1 and 10.");
            set.WeightKg = 0;
        }
        else
        {
            if (!validLoad)
                throw new ValidationException("Timed sets accept 0–500 kg of external load.");
            set.Intensity = 0;
        }
        set.Reps = 0;
    }

    private static readonly Dictionary<string, string> SplitDayNamesUk = new()
    {
        ["Legs"] = "Ноги",
        ["Back"] = "Спина",
        ["Chest"] = "Груди",
        ["Upper"] = "Верх тіла",
        ["Lower"] = "Низ тіла",
        ["Full Body"] = "Усе тіло",
        ["Training Day"] = "Тренувальний день"
    };

    // Saved workouts keep the English name ("Legs day") and are translated when shown.
    private static readonly Dictionary<string, string> SessionNamesUk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Legs day"] = "День ніг",
        ["Back day"] = "День спини",
        ["Chest day"] = "День грудей",
        ["Upper day"] = "День верху тіла",
        ["Lower day"] = "День низу тіла",
        ["Full Body day"] = "День на все тіло",
        ["Training Day"] = "Тренувальний день"
    };

    public static string SessionName(string savedName) => Localized.Pick(savedName, SessionNamesUk.GetValueOrDefault(savedName));

    public static List<SplitDayOption> SplitDayOptions(string? splitTypeName) => SplitDays(splitTypeName)
        .Select(day => new SplitDayOption(day, Localized.Pick(day, SplitDayNamesUk.GetValueOrDefault(day))))
        .ToList();

    public static List<string> SplitDays(string? splitTypeName)
    {
        var normalized = splitTypeName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Contains("legs-back-chest"))
            return ["Legs", "Back", "Chest"];
        if (normalized.Contains("upper-lower"))
            return ["Upper", "Lower"];
        if (normalized.Contains("full-body"))
            return ["Full Body"];
        return ["Training Day"];
    }

    public bool Delete(int userId, int sessionId)
    {
        var session = db.WorkoutSessions.Include(s => s.PerformedExercises)
            .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);
        if (session is null)
            return false;
        var exerciseIds = session.PerformedExercises.Select(e => e.ExerciseId).Distinct().ToList();
        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForAthletes(db, userId);
        db.WorkoutSessions.Remove(session);
        db.SaveChanges();
        foreach (var exerciseId in exerciseIds)
        {
            var record = db.PersonalRecords.FirstOrDefault(r => r.UserId == userId && r.ExerciseId == exerciseId);
            if (record is null)
                continue;
            var best = db.Sets.Where(s => s.PerformedExercise.ExerciseId == exerciseId && s.PerformedExercise.WorkoutSession.UserId == userId)
                .OrderByDescending(s => s.WeightKg).Select(s => new { s.WeightKg, s.PerformedExercise.WorkoutSession.SessionDate }).FirstOrDefault();
            if (best is null || best.WeightKg <= 0)
                db.PersonalRecords.Remove(record);
            else
            {
                record.MaxWeightKg = best.WeightKg;
                record.UpdatedAt = best.SessionDate;
            }
        }
        db.SaveChanges();
        transaction.Commit();
        return true;
    }
}
