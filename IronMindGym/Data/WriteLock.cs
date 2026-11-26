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

    /// <summary>Ids are locked in ascending order, so two requests never wait on each other.</summary>
    public static IDbContextTransaction BeginForAthletes(IronMindDbContext db, params int[] userIds)
    {
        var transaction = db.Database.BeginTransaction();
        foreach (var userId in userIds.Distinct().Order())
            db.Database.ExecuteSql($"SELECT pg_advisory_xact_lock({AthleteScope}, {userId})");
        return transaction;
    }

    public static IDbContextTransaction BeginForSeeding(IronMindDbContext db)
    {
        var transaction = db.Database.BeginTransaction();
        db.Database.ExecuteSql($"SELECT pg_advisory_xact_lock({SeedScope}, 0)");
        return transaction;
    }
}
