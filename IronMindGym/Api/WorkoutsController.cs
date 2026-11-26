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
    public ActionResult<WorkoutDetailsResponse> Details(int id) =>
        workouts.Details(UserId, id, UserId) is { } details ? details : NotFound();

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id) => workouts.Delete(UserId, id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/comments")]
    public ActionResult AddComment(int id, CommentRequest request) =>
        social.AddComment(UserId, UserId, id, request.Text) ? NoContent() : NotFound();
}
