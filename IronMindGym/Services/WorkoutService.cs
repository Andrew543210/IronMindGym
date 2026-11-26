using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutService(IronMindDbContext db, PersonalRecordService records)
{
    public WorkoutDetails? BuildDetails(int ownerId, int sessionId, int viewerId)
    {
        var session = db.WorkoutSessions.AsNoTracking()
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Sets).ThenInclude(set => set.SetType)
            .Include(s => s.SplitDay)
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
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new WorkoutComment { AuthorUsername = x.Author.Username, Text = x.Text, CreatedAt = x.CreatedAt })
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
    /// Saves a finished workout and recalculates the personal records and awards it affects.
    /// A retry with the same draft id returns the workout saved by the first request.
    /// </summary>
    public int Save(int currentUserId, SaveWorkoutRequest workout)
    {
        if (workout.Sets is null || workout.Sets.Count is < 1 or > 200 || workout.Sets.Any(s => s is null))
            throw new DomainValidationException("Add between 1 and 200 sets before saving.", "Sets");
        var exercises = ValidateSets(workout.Sets);
        if (workout.SplitDayId.HasValue && !db.SplitDays.Any(d => d.Id == workout.SplitDayId))
            throw new DomainValidationException("Choose a valid training day.", "SplitDayId");

        using var transaction = WriteLock.BeginForAthletes(db, currentUserId);
        if (workout.DraftId.HasValue)
        {
            var savedId = db.WorkoutSessions.Where(s => s.UserId == currentUserId && s.DraftId == workout.DraftId)
                .Select(s => (int?)s.Id).FirstOrDefault();
            if (savedId.HasValue)
                return savedId.Value;
            db.WorkoutDrafts.Where(d => d.UserId == currentUserId && d.DraftId == workout.DraftId).ExecuteDelete();
        }
        var session = new WorkoutSession
        {
            UserId = currentUserId,
            SessionDate = DateTime.UtcNow,
            DraftId = workout.DraftId,
            DurationMinutes = workout.DurationMinutes,
            SplitDayId = workout.SplitDayId,
            Rating = workout.ReviewRating,
            Notes = string.IsNullOrWhiteSpace(workout.ReviewComment) ? null : workout.ReviewComment.Trim()
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

        db.SaveChanges();
        records.Recalculate(currentUserId, exercises.Keys);
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
                throw new DomainValidationException("Choose valid exercises and set types.");
            if (set.RestSeconds is < 0 or > 3600)
                throw new DomainValidationException("Rest must be between 0 and 3600 seconds.");
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
                throw new DomainValidationException("Strength sets require 0–500 kg and 1–500 reps.");
            set.DurationSeconds = 0;
            set.Intensity = 0;
            return;
        }
        if (set.DurationSeconds is < 1 or > 86400)
            throw new DomainValidationException("Duration must be between 1 and 86400 seconds.");
        if (type == ExerciseType.Cardio)
        {
            if (set.Intensity is < 1 or > 10)
                throw new DomainValidationException("Cardio effort must be between 1 and 10.");
            set.WeightKg = 0;
        }
        else
        {
            if (!validLoad)
                throw new DomainValidationException("Timed sets accept 0–500 kg of external load.");
            set.Intensity = 0;
        }
        set.Reps = 0;
    }

    /// <summary>The title of a workout: its split day's, or a general training day's.</summary>
    public static string SessionName(string? workoutName, string? workoutNameUk) => workoutName is null
        ? Localized.Pick("Training Day", "Тренувальний день")
        : Localized.Pick(workoutName, workoutNameUk);

    /// <summary>A repeated delete, such as a double tap, finds nothing to delete and reports false.</summary>
    public bool Delete(int userId, int sessionId)
    {
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        var exerciseIds = db.PerformedExercises
            .Where(p => p.WorkoutSessionId == sessionId && p.WorkoutSession.UserId == userId)
            .Select(p => p.ExerciseId).Distinct().ToList();
        if (db.WorkoutSessions.Where(s => s.Id == sessionId && s.UserId == userId).ExecuteDelete() == 0)
            return false;
        records.Recalculate(userId, exerciseIds);
        db.SaveChanges();
        transaction.Commit();
        return true;
    }
}
