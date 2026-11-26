using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutService(IronMindDbContext db, TimeProvider clock, AthleteCalendars calendars, ExerciseCatalog catalog, PersonalRecordService records, AchievementService achievements)
{
    private const int HistoryPageSize = 20;

    /// <summary>The athlete's workouts, newest first, one page at a time.</summary>
    public WorkoutHistoryResponse History(int userId, int page)
    {
        page = Math.Clamp(page, 1, 10000);
        var rows = db.WorkoutSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Id)
            .Skip((page - 1) * HistoryPageSize)
            .Take(HistoryPageSize + 1)
            .Select(s => new WorkoutSessionListItem
            {
                Id = s.Id,
                SessionDate = s.SessionDate,
                Name = SessionName(s.SplitDay!.WorkoutName, s.SplitDay.WorkoutNameUk),
                DurationMinutes = s.DurationMinutes
            })
            .ToList();
        return new WorkoutHistoryResponse(rows.Take(HistoryPageSize).ToList(), page, rows.Count > HistoryPageSize);
    }

    /// <summary>What the logger offers: the athlete's split with its days, the set types and the exercise catalog.</summary>
    public WorkoutLoggerResponse LoggerOptions(int userId)
    {
        var activePlan = db.WorkoutPlans.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.SplitTypeId, Name = Localized.Pick(p.SplitType.Name, p.SplitType.NameUk) })
            .FirstOrDefault();
        var splitTypes = db.SplitTypes.AsNoTracking()
            .Where(s => activePlan == null || s.Id == activePlan.SplitTypeId)
            .Select(s => new { s.Id, s.Name, s.NameUk, Days = s.Days.OrderBy(d => d.Position).Select(d => new { d.Id, d.Name, d.NameUk }).ToList() })
            .AsEnumerable()
            .Select(s => new SplitTypeOption(s.Id, Localized.Pick(s.Name, s.NameUk),
                s.Days.Select(d => new SplitDayOption(d.Id, Localized.Pick(d.Name, d.NameUk))).ToList()))
            .OrderBy(s => s.Name, Localized.Order)
            .ToList();
        var setTypes = db.SetTypes.AsNoTracking().Select(s => new { s.Id, s.Code, s.Name, s.NameUk }).ToList();
        var defaultSetTypeId = setTypes.FirstOrDefault(s => s.Code == SetTypeCode.Working)?.Id ?? setTypes.FirstOrDefault()?.Id ?? 0;
        return new WorkoutLoggerResponse(
            activePlan?.Name,
            activePlan?.SplitTypeId,
            splitTypes,
            setTypes.Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk)))
                .OrderBy(s => s.Id == defaultSetTypeId ? 0 : 1).ThenBy(s => s.Name, Localized.Order).ToList(),
            defaultSetTypeId,
            catalog.MuscleGroups(),
            catalog.Items());
    }

    /// <summary>The last set the athlete logged for an exercise, to start the next one from.</summary>
    public PreviousSetResponse? PreviousSet(int userId, int exerciseId)
    {
        var previous = db.Sets.AsNoTracking()
            .Where(s => s.PerformedExercise.ExerciseId == exerciseId && s.PerformedExercise.WorkoutSession.UserId == userId)
            .OrderByDescending(s => s.PerformedExercise.WorkoutSession.SessionDate)
            .ThenByDescending(s => s.PerformedExercise.WorkoutSessionId)
            .ThenByDescending(s => s.PerformedExercise.OrderInWorkout)
            .ThenByDescending(s => s.SetNumber)
            .Select(s => new { s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds, s.SetTypeId, Date = s.PerformedExercise.WorkoutSession.SessionDate })
            .FirstOrDefault();
        return previous is null
            ? null
            : new PreviousSetResponse(previous.WeightKg, previous.Reps, previous.DurationSeconds, previous.Intensity,
                previous.RestSeconds, previous.SetTypeId, calendars.For(userId).DayOf(previous.Date));
    }

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
    public int Save(int userId, SaveWorkoutRequest workout)
    {
        var sets = CheckWorkout(workout);
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (workout.DraftId is { } draftId)
        {
            // A retried save returns the workout it already made; otherwise the draft is replaced by the workout.
            var savedId = db.WorkoutSessions.Where(s => s.UserId == userId && s.DraftId == draftId).Select(s => (int?)s.Id).FirstOrDefault();
            if (savedId.HasValue)
                return savedId.Value;
            db.WorkoutDrafts.Where(d => d.UserId == userId && d.DraftId == draftId).ExecuteDelete();
        }
        var session = NewSession(userId, workout, sets);
        db.WorkoutSessions.Add(session);
        db.SaveChanges();
        records.Recalculate(userId, session.PerformedExercises.Select(p => p.ExerciseId).Distinct());
        db.SaveChanges();
        achievements.AwardEarned(userId);
        transaction.Commit();
        return session.Id;
    }

    private List<(WorkoutSetInput Input, TrackedResult Result)> CheckWorkout(SaveWorkoutRequest workout)
    {
        if (workout.Sets is null || workout.Sets.Count is < 1 or > 200 || workout.Sets.Any(s => s is null))
            throw new DomainValidationException("Add between 1 and 200 sets before saving.", "Sets");
        var sets = CheckSets(workout.Sets);
        if (workout.SplitDayId.HasValue && !db.SplitDays.Any(d => d.Id == workout.SplitDayId))
            throw new DomainValidationException("Choose a valid training day.", "SplitDayId");
        return sets;
    }

    /// <summary>
    /// Consecutive sets of one exercise form one block, so supersets and circuits keep their logged order.
    /// Set numbers count per exercise across its blocks.
    /// </summary>
    private WorkoutSession NewSession(int userId, SaveWorkoutRequest workout, List<(WorkoutSetInput Input, TrackedResult Result)> sets)
    {
        var session = new WorkoutSession
        {
            UserId = userId,
            SessionDate = clock.UtcNow(),
            DraftId = workout.DraftId,
            DurationMinutes = workout.DurationMinutes,
            SplitDayId = workout.SplitDayId,
            Rating = workout.ReviewRating,
            Notes = string.IsNullOrWhiteSpace(workout.ReviewComment) ? null : workout.ReviewComment.Trim()
        };
        PerformedExercise? block = null;
        var setNumbers = new Dictionary<int, int>();
        foreach (var (set, result) in sets)
        {
            if (block?.ExerciseId != set.ExerciseId)
            {
                block = new PerformedExercise { ExerciseId = set.ExerciseId, WorkoutSession = session, OrderInWorkout = session.PerformedExercises.Count + 1 };
                session.PerformedExercises.Add(block);
            }
            setNumbers[set.ExerciseId] = setNumbers.GetValueOrDefault(set.ExerciseId) + 1;
            block.Sets.Add(new WorkoutSet
            {
                SetNumber = setNumbers[set.ExerciseId],
                SetTypeId = set.SetTypeId,
                WeightKg = result.WeightKg,
                Reps = result.Reps,
                DurationSeconds = result.DurationSeconds,
                Intensity = result.Intensity,
                RestSeconds = set.RestSeconds
            });
        }
        return session;
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
