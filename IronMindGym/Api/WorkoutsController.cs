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
    AthleteCalendars calendars) : ApiController
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
                Name = WorkoutService.SessionName(s.SplitDay!.WorkoutName, s.SplitDay.WorkoutNameUk),
                DurationMinutes = s.DurationMinutes
            })
            .ToList();
        return new WorkoutHistoryResponse(rows.Take(HistoryPageSize).ToList(), page, rows.Count > HistoryPageSize);
    }

    [HttpGet("logger")]
    public WorkoutLoggerResponse Logger()
    {
        var activePlan = db.WorkoutPlans.AsNoTracking()
            .Where(p => p.UserId == UserId)
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
        var defaultSetTypeId = setTypes.FirstOrDefault(s => s.Code == SetTypeCode.Working)?.Id
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
            previous.RestSeconds, previous.SetTypeId, calendars.For(UserId).DayOf(previous.Date));
    }

    [HttpPost]
    [RequestSizeLimit(150_000)]
    public SavedWorkoutResponse Save(SaveWorkoutRequest request) => new(workouts.Save(UserId, request));

    [HttpGet("{id:int}")]
    public ActionResult<WorkoutDetailsResponse> Details(int id)
    {
        var details = workouts.BuildDetails(UserId, id, UserId);
        if (details is null)
            return NotFound();
        details.RemainingWorkoutsThisWeek = Math.Max(
            db.WorkoutPlans.WorkoutsPerWeek(UserId) - db.WorkoutSessions.WorkoutsThisWeek(UserId, calendars.For(UserId)), 0);
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

public sealed record PreviousSetResponse(double WeightKg, int Reps, int DurationSeconds, int Intensity, int RestSeconds, int SetTypeId, DateOnly LocalDate);

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
            WorkoutService.SessionName(session.SplitDay?.WorkoutName, session.SplitDay?.WorkoutNameUk),
            session.SessionDate,
            session.DurationMinutes,
            session.Rating,
            session.Notes,
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
