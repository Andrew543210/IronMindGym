using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IronMindGym.Data;

// Serializes an athlete's check-then-insert writes, so a double tap or a retry cannot save twice.
// The lock belongs to the transaction, and PostgreSQL releases it when the transaction ends.
public static class WriteLock
{
    private const int AthleteScope = 1;
    private const int SeedScope = 2;

    public static Task<IDbContextTransaction> BeginForAthleteAsync(IronMindDbContext db, int userId, CancellationToken cancellationToken) =>
        BeginForAthletesAsync(db, [userId], cancellationToken);

    // Ids are locked in ascending order, so two requests never deadlock.
    public static async Task<IDbContextTransaction> BeginForAthletesAsync(IronMindDbContext db, int[] userIds, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var userId in userIds.Distinct().Order())
                await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AthleteScope}, {userId})", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public static async Task<IDbContextTransaction> BeginForSeedingAsync(IronMindDbContext db, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SeedScope}, 0)", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
