using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/workouts")]
public class WorkoutsController(
    IronMindDbContext db,
    WorkoutService workouts,
    WorkoutSocialService social,
    CurrentCalendar calendar,
    ILogger<WorkoutsController> logger) : ApiController
{
    private const int HistoryPageSize = 20;

    [HttpGet]
    public WorkoutHistoryResponse History(int page = 1)
    {
        page = Math.Clamp(page, 1, 10000);
        var rows = db.WorkoutSessions.AsNoTracking()
            .Where(s => s.UserId == UserId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Id)
            .Skip((page - 1) * HistoryPageSize)
            .Take(HistoryPageSize + 1)
            .Select(s => new WorkoutSessionListItem
            {
                Id = s.Id,
                SessionDate = s.SessionDate,
                Name = WorkoutService.SessionName(s.Name),
                DurationMinutes = s.DurationMinutes
            })
            .ToList();
        return new WorkoutHistoryResponse(rows.Take(HistoryPageSize).ToList(), page, rows.Count > HistoryPageSize);
    }

    [HttpGet("logger")]
    public WorkoutLoggerResponse Logger()
    {
        var activePlan = db.WorkoutPlans.AsNoTracking()
            .Where(p => p.UserId == UserId && p.IsActive)
            .Select(p => new { p.SplitTypeId, Name = Localized.Pick(p.SplitType.Name, p.SplitType.NameUk) })
            .FirstOrDefault();
        var splitTypes = db.SplitTypes.AsNoTracking()
            .Where(s => activePlan == null || s.Id == activePlan.SplitTypeId)
            .Select(s => new { s.Id, s.Name, s.NameUk })
            .AsEnumerable()
            .Select(s => new SplitTypeOption(s.Id, Localized.Pick(s.Name, s.NameUk), WorkoutService.SplitDayOptions(s.Name)))
            .OrderBy(s => s.Name, Localized.Order)
            .ToList();

        var setTypes = db.SetTypes.AsNoTracking().Select(s => new { s.Id, s.Name, s.NameUk }).ToList();
        var defaultSetTypeId = setTypes.FirstOrDefault(s => s.Name.Equals("Working", StringComparison.OrdinalIgnoreCase))?.Id
            ?? setTypes.FirstOrDefault()?.Id ?? 0;

        return new WorkoutLoggerResponse(
            activePlan?.Name,
            activePlan?.SplitTypeId,
            splitTypes,
            setTypes.Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk)))
                .OrderBy(s => s.Id == defaultSetTypeId ? 0 : 1).ThenBy(s => s.Name, Localized.Order).ToList(),
            defaultSetTypeId,
            db.MuscleGroups.AsNoTracking().Select(g => new OptionResponse(g.Id, Localized.Pick(g.Name, g.NameUk)))
                .AsEnumerable().OrderBy(g => g.Name, Localized.Order).ToList(),
            ExerciseCatalog.Items(db.Exercises));
    }

    [HttpGet("previous-set")]
    public ActionResult<PreviousSetResponse> PreviousSet(int exerciseId)
    {
        var previous = db.Sets.AsNoTracking()
            .Where(s => s.PerformedExercise.ExerciseId == exerciseId && s.PerformedExercise.WorkoutSession.UserId == UserId)
            .OrderByDescending(s => s.PerformedExercise.WorkoutSession.SessionDate)
            .ThenByDescending(s => s.PerformedExercise.WorkoutSessionId)
            .ThenByDescending(s => s.PerformedExercise.OrderInWorkout)
            .ThenByDescending(s => s.SetNumber)
            .Select(s => new { s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds, s.SetTypeId, Date = s.PerformedExercise.WorkoutSession.SessionDate })
            .FirstOrDefault();
        if (previous is null)
            return NoContent();
        return new PreviousSetResponse(previous.WeightKg, previous.Reps, previous.DurationSeconds, previous.Intensity,
            previous.RestSeconds, previous.SetTypeId, calendar.Format(previous.Date, "yyyy-MM-dd"));
    }

    [HttpPost]
    [RequestSizeLimit(150_000)]
    public ActionResult<SavedWorkoutResponse> Save(SaveWorkoutRequest request)
    {
        try
        {
            return new SavedWorkoutResponse(workouts.Save(UserId, request));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not save workout for user {UserId}", UserId);
            return Rejected("Could not save this workout. Please try again.");
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<WorkoutDetailsResponse> Details(int id)
    {
        var details = workouts.BuildDetails(UserId, id, UserId);
        if (details is null)
            return NotFound();
        var weeklyTarget = db.WorkoutPlans.Where(w => w.UserId == UserId && w.IsActive).Select(w => w.WorkoutsPerWeek).FirstOrDefault();
        var weekStart = calendar.Value.WeekStartUtc(DateTime.UtcNow);
        var doneThisWeek = db.WorkoutSessions.Count(s => s.UserId == UserId && s.SessionDate >= weekStart);
        details.RemainingWorkoutsThisWeek = Math.Max(weeklyTarget - doneThisWeek, 0);
        return WorkoutDetailsResponse.From(details);
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id) => workouts.Delete(UserId, id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/comments")]
    public ActionResult AddComment(int id, CommentRequest request) =>
        social.AddComment(UserId, UserId, id, request.Text) ? NoContent() : NotFound();
}

public sealed record WorkoutHistoryResponse(List<WorkoutSessionListItem> Sessions, int Page, bool HasOlderSessions);

public sealed record SplitTypeOption(int Id, string Name, List<SplitDayOption> Days);

public sealed record WorkoutLoggerResponse(
    string? ActiveSplitTypeName,
    int? ActiveSplitTypeId,
    List<SplitTypeOption> SplitTypes,
    List<OptionResponse> SetTypes,
    int DefaultSetTypeId,
    List<OptionResponse> MuscleGroups,
    List<ExerciseCatalogItem> Exercises);

public sealed record PreviousSetResponse(double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds, int SetTypeId, string LocalDate);

public sealed record SavedWorkoutResponse(int Id);

public sealed record CommentRequest(string? Text);

public sealed record WorkoutSetResponse(int SetNumber, string? SetTypeName, double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds);

public sealed record PerformedExerciseResponse(string ExerciseName, ExerciseType ExerciseType, List<WorkoutSetResponse> Sets);

public sealed record WorkoutDetailsResponse(
    int Id,
    string Name,
    DateTime SessionDate,
    int DurationMinutes,
    int? Rating,
    string? Notes,
    List<PerformedExerciseResponse> Exercises,
    double AverageRestSeconds,
    int? RemainingWorkoutsThisWeek,
    List<ExerciseWorkoutSummary> ExerciseStats,
    bool IsOwner,
    int LikesCount,
    bool IsLikedByCurrentUser,
    List<WorkoutComment> Comments)
{
    public static WorkoutDetailsResponse From(WorkoutDetails details)
    {
        var session = details.Session;
        return new WorkoutDetailsResponse(
            session.Id,
            WorkoutService.SessionName(session.Name),
            session.SessionDate,
            session.DurationMinutes,
            session.Review?.Rating,
            string.IsNullOrWhiteSpace(session.Review?.Comment) ? null : session.Review.Comment,
            session.PerformedExercises.OrderBy(p => p.OrderInWorkout).Select(p => new PerformedExerciseResponse(
                Localized.Pick(p.Exercise.Name, p.Exercise.NameUk),
                p.Exercise.Type,
                p.Sets.OrderBy(s => s.SetNumber)
                    .Select(s => new WorkoutSetResponse(s.SetNumber, s.SetType is null ? null : Localized.Pick(s.SetType.Name, s.SetType.NameUk), s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds))
                    .ToList())).ToList(),
            details.AverageRestSeconds,
            details.RemainingWorkoutsThisWeek,
            details.ExerciseStats,
            details.IsOwner,
            details.LikesCount,
            details.IsLikedByCurrentUser,
            details.Comments);
    }
}
