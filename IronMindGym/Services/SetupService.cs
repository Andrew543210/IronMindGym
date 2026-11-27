using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>The first setup: bodyweight, height, the starting lifts and the training plan.</summary>
public sealed class SetupService(IronMindDbContext db, AthleteCalendars calendars, AchievementService achievements)
{
    public async Task<SetupFormResponse> FormAsync(int userId, CancellationToken cancellationToken) =>
        new(await SplitTypesAsync(cancellationToken), await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken));

    /// <summary>The split types in the request language, in their catalog order.</summary>
    public Task<List<OptionResponse>> SplitTypesAsync(CancellationToken cancellationToken) => db.SplitTypes.AsNoTracking().OrderBy(s => s.Id)
        .Select(s => new OptionResponse(s.Id, Localized.Pick(s.Name, s.NameUk))).ToListAsync(cancellationToken);

    /// <summary>Saves the setup once. A repeated request, such as a double tap, changes nothing.</summary>
    public async Task SaveAsync(int userId, SetupRequest request, CancellationToken cancellationToken)
    {
        if (!await db.SplitTypes.AnyAsync(s => s.Id == request.SplitTypeId, cancellationToken))
            throw new DomainValidationException("Choose a valid training split.", nameof(request.SplitTypeId));

        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken))
            return;

        var user = await db.Users.Include(u => u.Benchmarks).Include(u => u.Profile).SingleAsync(u => u.Id == userId, cancellationToken);
        user.Profile ??= new Profile { UserId = userId };
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        if (user.Profile.TimeZone is null && AthleteCalendar.TryFindZone(request.TimeZone, out _))
            user.Profile.TimeZone = request.TimeZone;
        user.Profile.HeightCm = request.HeightCm;
        user.Benchmarks.BenchPressKg = request.BenchPressKg;
        user.Benchmarks.SquatKg = request.SquatKg;
        user.Benchmarks.DeadliftKg = request.DeadliftKg;
        var today = calendars.InZone(user.Profile.TimeZone).Today;
        var weighIn = await db.WeightLogs.FirstOrDefaultAsync(w => w.UserId == userId && w.Date == today, cancellationToken);
        if (weighIn is null)
            db.WeightLogs.Add(new WeightLog { UserId = userId, Date = today, ValueKg = request.WeightKg });
        else
            weighIn.ValueKg = request.WeightKg;
        var plan = await db.WorkoutPlans.FindAsync([userId], cancellationToken);
        if (plan is null)
            db.WorkoutPlans.Add(plan = new WorkoutPlan { UserId = userId });
        plan.WorkoutsPerWeek = request.WorkoutsPerWeek;
        plan.SplitTypeId = request.SplitTypeId;
        await db.SaveChangesAsync(cancellationToken);
        calendars.Forget(userId);
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
