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
    public async Task<WorkoutHistoryResponse> HistoryAsync(int userId, int page, CancellationToken cancellationToken)
    {
        page = Math.Clamp(page, 1, 10000);
        var rows = await db.WorkoutSessions.AsNoTracking()
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
            .ToListAsync(cancellationToken);
        return new WorkoutHistoryResponse(rows.Take(HistoryPageSize).ToList(), page, rows.Count > HistoryPageSize);
    }

    /// <summary>What the logger offers: the athlete's split with its days, the set types and the exercise catalog.</summary>
    public async Task<WorkoutLoggerResponse> LoggerOptionsAsync(int userId, CancellationToken cancellationToken)
    {
        var activePlan = await db.WorkoutPlans.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.SplitTypeId, Name = Localized.Pick(p.SplitType.Name, p.SplitType.NameUk) })
            .FirstOrDefaultAsync(cancellationToken);
        var splits = await db.SplitTypes.AsNoTracking()
            .Where(s => activePlan == null || s.Id == activePlan.SplitTypeId)
            .Select(s => new { s.Id, s.Name, s.NameUk, Days = s.Days.OrderBy(d => d.Position).Select(d => new { d.Id, d.Name, d.NameUk }).ToList() })
            .ToListAsync(cancellationToken);
        var setTypes = await db.SetTypes.AsNoTracking().Select(s => new { s.Id, s.Code, s.Name, s.NameUk }).ToListAsync(cancellationToken);
        var defaultSetTypeId = setTypes.FirstOrDefault(s => s.Code == SetTypeCode.Working)?.Id ?? setTypes.FirstOrDefault()?.Id ?? 0;
        return new WorkoutLoggerResponse(
            activePlan?.Name,
            activePlan?.SplitTypeId,
            splits
                .Select(s => new SplitTypeOption(s.Id, Localized.Pick(s.Name, s.NameUk),
                    s.Days.Select(d => new SplitDayOption(d.Id, Localized.Pick(d.Name, d.NameUk))).ToList()))
                .OrderBy(s => s.Name, Localized.Order)
                .ToList(),
            setTypes.Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk)))
                .OrderBy(s => s.Id == defaultSetTypeId ? 0 : 1).ThenBy(s => s.Name, Localized.Order).ToList(),
            defaultSetTypeId,
            await catalog.ItemsAsync(cancellationToken));
    }

    /// <summary>The last set the athlete logged for an exercise, to start the next one from.</summary>
    public async Task<PreviousSetResponse?> PreviousSetAsync(int userId, int exerciseId, CancellationToken cancellationToken)
    {
        var previous = await db.Sets.AsNoTracking()
            .Where(s => s.PerformedExercise.ExerciseId == exerciseId && s.PerformedExercise.WorkoutSession.UserId == userId)
            .OrderByDescending(s => s.PerformedExercise.WorkoutSession.SessionDate)
            .ThenByDescending(s => s.PerformedExercise.WorkoutSessionId)
            .ThenByDescending(s => s.PerformedExercise.OrderInWorkout)
            .ThenByDescending(s => s.SetNumber)
            .Select(s => new { s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds, s.SetTypeId, Date = s.PerformedExercise.WorkoutSession.SessionDate })
            .FirstOrDefaultAsync(cancellationToken);
        if (previous is null)
            return null;
        var calendar = await calendars.ForAsync(userId, cancellationToken);
        return new PreviousSetResponse(previous.WeightKg, previous.Reps, previous.DurationSeconds, previous.Intensity,
            previous.RestSeconds, previous.SetTypeId, calendar.DayOf(previous.Date));
    }

    /// <summary>
    /// A saved workout as its owner or a friend sees it. The owner also sees how many planned workouts are
    /// left this week.
    /// </summary>
    public async Task<WorkoutDetailsResponse?> DetailsAsync(int ownerId, int sessionId, int viewerId, CancellationToken cancellationToken)
    {
        var session = await db.WorkoutSessions.AsNoTracking()
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises).ThenInclude(pe => pe.Sets).ThenInclude(set => set.SetType)
            .Include(s => s.SplitDay)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == ownerId, cancellationToken);
        if (session is null)
            return null;
        var isOwner = viewerId == ownerId;
        int? remainingThisWeek = null;
        if (isOwner)
        {
            var calendar = await calendars.ForAsync(ownerId, cancellationToken);
            var planned = await db.WorkoutPlans.WorkoutsPerWeekAsync(ownerId, cancellationToken);
            remainingThisWeek = Math.Max(planned - await db.WorkoutSessions.WorkoutsThisWeekAsync(ownerId, calendar, cancellationToken), 0);
        }
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
            remainingThisWeek,
            await BuildExerciseStatsAsync(session, cancellationToken),
            isOwner,
            await db.ActivityLikes.CountAsync(x => x.WorkoutSessionId == sessionId, cancellationToken),
            await db.ActivityLikes.AnyAsync(x => x.WorkoutSessionId == sessionId && x.UserId == viewerId, cancellationToken),
            await db.FriendComments.AsNoTracking()
                .Where(x => x.WorkoutSessionId == sessionId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new WorkoutComment { AuthorUsername = x.Author.Username, Text = x.Text, CreatedAt = x.CreatedAt })
                .ToListAsync(cancellationToken));
    }

    /// <summary>
    /// Heaviest external load per exercise compared with the best load logged before this workout.
    /// Later workouts never change the comparison. Only strength exercises have load records.
    /// </summary>
    public async Task<List<ExerciseWorkoutSummary>> BuildExerciseStatsAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        var exerciseIds = session.PerformedExercises.Select(p => p.ExerciseId).Distinct().ToList();
        var previousRecords = await db.Sets.AsNoTracking()
            .Where(s => s.PerformedExercise.WorkoutSession.UserId == session.UserId
                && exerciseIds.Contains(s.PerformedExercise.ExerciseId)
                && s.PerformedExercise.Exercise.Type == ExerciseType.StrengthTraining
                && (s.PerformedExercise.WorkoutSession.SessionDate < session.SessionDate
                    || (s.PerformedExercise.WorkoutSession.SessionDate == session.SessionDate
                        && s.PerformedExercise.WorkoutSessionId < session.Id)))
            .GroupBy(s => s.PerformedExercise.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Weight = g.Max(s => s.WeightKg) })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Weight, cancellationToken);
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
    public async Task<int> SaveAsync(int userId, SaveWorkoutRequest workout, CancellationToken cancellationToken)
    {
        var sets = await CheckWorkoutAsync(workout, cancellationToken);
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (workout.DraftId is { } draftId)
        {
            // A retried save returns the workout it already made; otherwise the draft is replaced by the workout.
            var savedId = await db.WorkoutSessions.Where(s => s.UserId == userId && s.DraftId == draftId)
                .Select(s => (int?)s.Id).FirstOrDefaultAsync(cancellationToken);
            if (savedId.HasValue)
                return savedId.Value;
            await db.WorkoutDrafts.Where(d => d.UserId == userId && d.DraftId == draftId).ExecuteDeleteAsync(cancellationToken);
        }
        var session = NewSession(userId, workout, sets);
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await records.RecalculateAsync(userId, session.PerformedExercises.Select(p => p.ExerciseId), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session.Id;
    }

    private async Task<List<(WorkoutSetInput Input, TrackedResult Result)>> CheckWorkoutAsync(SaveWorkoutRequest workout,
        CancellationToken cancellationToken)
    {
        if (workout.Sets is null || workout.Sets.Count is < 1 or > 200 || workout.Sets.Any(s => s is null))
            throw new DomainValidationException("Add between 1 and 200 sets before saving.", "Sets");
        var sets = await CheckSetsAsync(workout.Sets, cancellationToken);
        if (workout.SplitDayId.HasValue && !await db.SplitDays.AnyAsync(d => d.Id == workout.SplitDayId, cancellationToken))
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
    private async Task<List<(WorkoutSetInput Input, TrackedResult Result)>> CheckSetsAsync(List<WorkoutSetInput> sets,
        CancellationToken cancellationToken)
    {
        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList();
        var types = await db.Exercises.AsNoTracking().Where(e => exerciseIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Type, cancellationToken);
        var setTypeIds = await db.SetTypes.Select(s => s.Id).ToHashSetAsync(cancellationToken);
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
    public async Task<bool> DeleteAsync(int userId, int sessionId, CancellationToken cancellationToken)
    {
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var exerciseIds = await db.PerformedExercises
            .Where(p => p.WorkoutSessionId == sessionId && p.WorkoutSession.UserId == userId)
            .Select(p => p.ExerciseId).Distinct().ToListAsync(cancellationToken);
        if (await db.WorkoutSessions.Where(s => s.Id == sessionId && s.UserId == userId).ExecuteDeleteAsync(cancellationToken) == 0)
            return false;
        await records.RecalculateAsync(userId, exerciseIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
