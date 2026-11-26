using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutService(IronMindDbContext db, TimeProvider clock, AthleteCalendars calendars, PersonalRecordService records, AchievementService achievements)
{
    /// <summary>
    /// A saved workout as its owner or a friend sees it. The owner also sees how many planned workouts are
    /// left this week.
    /// </summary>
    public WorkoutDetailsResponse? Details(int ownerId, int sessionId, int viewerId)
    {
        var session = db.WorkoutSessions.AsNoTracking()
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Sets).ThenInclude(set => set.SetType)
            .Include(s => s.SplitDay)
            .FirstOrDefault(s => s.Id == sessionId && s.UserId == ownerId);
        if (session is null)
            return null;
        var isOwner = viewerId == ownerId;
        var sets = session.PerformedExercises.SelectMany(pe => pe.Sets).ToList();
        return new WorkoutDetailsResponse(
            session.Id,
            SessionName(session.SplitDay?.WorkoutName, session.SplitDay?.WorkoutNameUk),
            session.SessionDate,
            session.DurationMinutes,
            session.Rating,
            session.Notes,
            session.PerformedExercises.OrderBy(p => p.OrderInWorkout).Select(p => new PerformedExerciseResponse(
                Localized.Pick(p.Exercise.Name, p.Exercise.NameUk),
                p.Exercise.Type,
                p.Sets.OrderBy(s => s.SetNumber)
                    .Select(s => new WorkoutSetResponse(s.SetNumber, s.SetType is null ? null : Localized.Pick(s.SetType.Name, s.SetType.NameUk),
                        s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds))
                    .ToList())).ToList(),
            sets.Count > 0 ? Math.Round(sets.Average(s => s.RestSeconds), 1) : 0,
            isOwner
                ? Math.Max(db.WorkoutPlans.WorkoutsPerWeek(ownerId) - db.WorkoutSessions.WorkoutsThisWeek(ownerId, calendars.For(ownerId)), 0)
                : null,
            BuildExerciseStats(session),
            isOwner,
            db.ActivityLikes.Count(x => x.WorkoutSessionId == sessionId),
            db.ActivityLikes.Any(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId),
            db.FriendComments.AsNoTracking()
                .Where(x => x.WorkoutSessionId == sessionId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new WorkoutComment { AuthorUsername = x.Author.Username, Text = x.Text, CreatedAt = x.CreatedAt })
                .ToList());
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
        var checkedSets = CheckSets(workout.Sets);
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
            SessionDate = clock.UtcNow(),
            DraftId = workout.DraftId,
            DurationMinutes = workout.DurationMinutes,
            SplitDayId = workout.SplitDayId,
            Rating = workout.ReviewRating,
            Notes = string.IsNullOrWhiteSpace(workout.ReviewComment) ? null : workout.ReviewComment.Trim()
        };

        // Keep contiguous blocks so supersets and circuits retain their logged order.
        PerformedExercise? performedExercise = null;
        var setNumbers = new Dictionary<int, int>();
        foreach (var (set, result) in checkedSets)
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
                WeightKg = result.WeightKg,
                Reps = result.Reps,
                SetTypeId = set.SetTypeId,
                DurationSeconds = result.DurationSeconds,
                Intensity = result.Intensity,
                RestSeconds = set.RestSeconds
            });
        }
        db.WorkoutSessions.Add(session);

        db.SaveChanges();
        records.Recalculate(currentUserId, checkedSets.Select(s => s.Input.ExerciseId).Distinct());
        db.SaveChanges();
        achievements.AwardEarned(currentUserId);
        transaction.Commit();
        return session.Id;
    }

    /// <summary>Checks every set against its exercise and returns the values each one keeps.</summary>
    private List<(WorkoutSetInput Input, TrackedResult Result)> CheckSets(List<WorkoutSetInput> sets)
    {
        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList();
        var types = db.Exercises.AsNoTracking().Where(e => exerciseIds.Contains(e.Id)).ToDictionary(e => e.Id, e => e.Type);
        var setTypeIds = db.SetTypes.Select(s => s.Id).ToHashSet();
        return sets.Select(set =>
        {
            if (!types.TryGetValue(set.ExerciseId, out var type) || !setTypeIds.Contains(set.SetTypeId))
                throw new DomainValidationException("Choose valid exercises and set types.");
            TrackingRules.CheckRest(set.RestSeconds);
            return (set, TrackingRules.Check(type, set.WeightKg, set.Reps, set.DurationSeconds, set.Intensity));
        }).ToList();
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
