using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IronMindGym.Data;

/// <summary>
/// Serializes writes that check existing rows before inserting and recalculate an athlete's
/// records and awards. Without it, two parallel requests (a double tap, a retry after a lost
/// response) could both see "nothing saved yet" and both insert.
/// PostgreSQL releases these locks when the surrounding transaction ends.
/// </summary>
public static class WriteLock
{
    private const int AthleteScope = 1;
    private const int SeedScope = 2;

    /// <summary>Call inside a transaction. Ids are locked in ascending order, so two requests never wait on each other.</summary>
    public static void ForAthletes(IronMindDbContext db, params int[] userIds)
    {
        foreach (var userId in userIds.Distinct().Order())
            db.Database.ExecuteSql($"SELECT pg_advisory_xact_lock({AthleteScope}, {userId})");
    }

    public static void ForSeeding(IronMindDbContext db) =>
        db.Database.ExecuteSql($"SELECT pg_advisory_xact_lock({SeedScope}, 0)");
}

public static class DbErrors
{
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
