using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>The first setup: bodyweight, height, the starting lifts and the training plan.</summary>
public sealed class SetupService(IronMindDbContext db, AthleteCalendars calendars, AchievementService achievements)
{
    public SetupFormResponse Form(int userId) => new(SplitTypes(), AthleteSetup.IsComplete(db, userId));

    /// <summary>The split types in the request language, in their catalog order.</summary>
    public List<OptionResponse> SplitTypes() => db.SplitTypes.AsNoTracking().OrderBy(s => s.Id)
        .Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk))).ToList();

    /// <summary>Saves the setup once. A repeated request, such as a double tap, changes nothing.</summary>
    public void Save(int userId, SetupRequest request)
    {
        if (!db.SplitTypes.Any(s => s.Id == request.SplitTypeId))
            throw new DomainValidationException("Choose a valid training split.", nameof(request.SplitTypeId));

        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (AthleteSetup.IsComplete(db, userId))
            return;

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
        calendars.Forget(userId);
        achievements.AwardEarned(userId);
        transaction.Commit();
    }
}
