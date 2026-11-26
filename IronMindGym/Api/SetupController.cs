using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/setup")]
public class SetupController(IronMindDbContext db, AthleteCalendars calendars, AchievementService achievements) : ApiController
{
    [HttpGet]
    public SetupFormResponse Form() => new(
        db.SplitTypes.AsNoTracking().OrderBy(s => s.Id).Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk))).ToList(),
        AthleteSetup.IsComplete(db, UserId));

    [HttpPost]
    public ActionResult Save(SetupRequest request)
    {
        if (!db.SplitTypes.Any(s => s.Id == request.SplitTypeId))
            throw new DomainValidationException("Choose a valid training split.", nameof(request.SplitTypeId));

        var userId = UserId;
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (AthleteSetup.IsComplete(db, userId))
            return NoContent();

        var user = db.Users.Include(u => u.Benchmarks).Include(u => u.Profile).Single(u => u.Id == userId);
        user.Profile ??= new Profile { UserId = userId };
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        if (user.Profile.TimeZone is null && AthleteCalendar.TryFindZone(request.TimeZone, out _))
            user.Profile.TimeZone = request.TimeZone;
        user.Profile.HeightCm = request.HeightCm;
        user.Benchmarks.BenchPressKg = request.BenchPressKg;
        user.Benchmarks.SquatKg = request.SquatKg;
        user.Benchmarks.DeadliftKg = request.DeadliftKg;
        var today = calendars.InZone(user.Profile.TimeZone).Today;
        var weighIn = db.WeightLogs.FirstOrDefault(w => w.UserId == userId && w.Date == today);
        if (weighIn is null)
            db.WeightLogs.Add(new WeightLog { UserId = userId, Date = today, ValueKg = request.WeightKg });
        else
            weighIn.ValueKg = request.WeightKg;
        var plan = db.WorkoutPlans.Find(userId);
        if (plan is null)
            db.WorkoutPlans.Add(plan = new WorkoutPlan { UserId = userId });
        plan.WorkoutsPerWeek = request.WorkoutsPerWeek;
        plan.SplitTypeId = request.SplitTypeId;
        db.SaveChanges();
        achievements.AwardEarned(userId);
        transaction.Commit();
        return NoContent();
    }
}

public sealed record SetupFormResponse(List<OptionResponse> SplitTypes, bool AlreadyCompleted);
