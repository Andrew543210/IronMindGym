using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IronMindGym.Data;

/// <summary>
/// Serializes writes that check existing rows before inserting and recalculate an athlete's
/// records and awards. Without it, two parallel requests (a double tap, a retry after a lost
/// response) could both see "nothing saved yet" and both insert.
/// The locks belong to the transaction that is started with them, and PostgreSQL releases them when it ends.
/// </summary>
public static class WriteLock
{
    private const int AthleteScope = 1;
    private const int SeedScope = 2;

    public static Task<IDbContextTransaction> BeginForAthleteAsync(IronMindDbContext db, int userId, CancellationToken cancellationToken) =>
        BeginForAthletesAsync(db, [userId], cancellationToken);

    /// <summary>Ids are locked in ascending order, so two requests never wait on each other.</summary>
    public static async Task<IDbContextTransaction> BeginForAthletesAsync(IronMindDbContext db, int[] userIds, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var userId in userIds.Distinct().Order())
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AthleteScope}, {userId})", cancellationToken);
        return transaction;
    }

    /// <summary>Two instances starting together seed one after the other.</summary>
    public static async Task<IDbContextTransaction> BeginForSeedingAsync(IronMindDbContext db, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SeedScope}, 0)", cancellationToken);
        return transaction;
    }
}
