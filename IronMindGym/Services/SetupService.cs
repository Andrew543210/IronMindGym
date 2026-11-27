using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
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
        await AthleteSetup.EnsureSplitExistsAsync(db, request.SplitTypeId, cancellationToken);
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken))
            return;

        var user = await db.Users.Include(u => u.Benchmarks).Include(u => u.Profile).SingleAsync(u => u.Id == userId, cancellationToken);
        await AthleteSetup.ApplyAsync(db, user, request, cancellationToken);
        if (user.Profile!.TimeZone is null && AthleteCalendar.TryFindZone(request.TimeZone, out _))
            user.Profile.TimeZone = request.TimeZone;
        await AthleteSetup.SaveWeighInAsync(db, userId, request.WeightKg, calendars.InZone(user.Profile.TimeZone).Today, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        calendars.Forget(userId);
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
