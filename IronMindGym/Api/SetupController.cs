using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/setup")]
public class SetupController(IronMindDbContext db) : ApiController
{
    [HttpGet]
    public SetupFormResponse Form() => new(
        db.SplitTypes.AsNoTracking().OrderBy(s => s.Id).Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk))).ToList(),
        AthleteSetup.IsComplete(db, UserId));

    [HttpPost]
    public ActionResult Save(SetupRequest request)
    {
        if (!db.SplitTypes.Any(s => s.Id == request.SplitTypeId))
            return Rejected("Choose a valid training split.", nameof(request.SplitTypeId));

        var userId = UserId;
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (AthleteSetup.IsComplete(db, userId))
            return NoContent();

        var user = db.Users.Include(u => u.Benchmarks).Include(u => u.Profile).Single(u => u.Id == userId);
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        if (user.Profile is { TimeZone: null } && AthleteCalendar.TryFindZone(request.TimeZone, out _))
            user.Profile.TimeZone = request.TimeZone;
        user.Benchmarks.BenchPressKg = request.BenchPressKg;
        user.Benchmarks.SquatKg = request.SquatKg;
        user.Benchmarks.DeadliftKg = request.DeadliftKg;
        db.AnthropometryLogs.Add(new Anthropometry
        {
            UserId = userId,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            RecordedAt = DateTime.UtcNow
        });
        foreach (var plan in db.WorkoutPlans.Where(p => p.UserId == userId && p.IsActive))
            plan.IsActive = false;
        db.WorkoutPlans.Add(new WorkoutPlan
        {
            UserId = userId,
            WorkoutsPerWeek = request.WorkoutsPerWeek,
            SplitTypeId = request.SplitTypeId,
            IsActive = true
        });
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
        transaction.Commit();
        return NoContent();
    }
}

public sealed record SetupFormResponse(List<OptionResponse> SplitTypes, bool AlreadyCompleted);
