using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IronMindGym.Tests.Data;

/// <summary>The database refuses rows that break its invariants, whatever code writes them.</summary>
public class SchemaRulesTests
{
    private static PostgresException Refused(TestApplication app, string sql) =>
        app.Query(db => Assert.Throws<PostgresException>(() => db.Database.ExecuteSqlRaw(sql)));

    private static int AddUsers(TestApplication app) => app.Query(db => db.Database.ExecuteSqlRaw("""
        INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "SecurityStamp") VALUES
            (1, 'one', 'one@example.com', 'hash', 'stamp'),
            (2, 'two', 'two@example.com', 'hash', 'stamp'),
            (3, 'three', 'three@example.com', 'hash', 'stamp')
        """));

    [Theory]
    [InlineData(2, 1, 2, "Pending")]
    [InlineData(1, 2, 3, "Pending")]
    [InlineData(1, 2, 1, "Blocked")]
    public void AFriendshipNeedsAnOrderedPairARequesterFromItAndAKnownStatus(int user1, int user2, int requester, string status)
    {
        using var app = new TestApplication();
        AddUsers(app);

        var refused = Refused(app, $"""
            INSERT INTO "Friendships" ("User1Id", "User2Id", "RequestedByUserId", "Status") VALUES ({user1}, {user2}, {requester}, '{status}')
            """);

        Assert.Equal(PostgresErrorCodes.CheckViolation, refused.SqlState);
    }

    [Theory]
    [InlineData("""INSERT INTO "WeightLogs" ("UserId", "Date", "ValueKg") VALUES (1, '2026-11-01', 0)""")]
    [InlineData("""INSERT INTO "DailyNutritions" ("UserId", "Date", "CaloriesKcal", "ProteinG", "FatG", "CarbsG") VALUES (1, '2026-11-01', -1, 0, 0, 0)""")]
    [InlineData("""INSERT INTO "Profiles" ("UserId", "Age", "AccountCreatedAt", "EnergyEquation") VALUES (1, 30, now(), 'Other')""")]
    [InlineData("""INSERT INTO "Profiles" ("UserId", "Age", "AccountCreatedAt", "EnergyEquation", "CalorieTarget") VALUES (1, 30, now(), 'None', 0)""")]
    [InlineData("""INSERT INTO "WorkoutPlans" ("UserId", "WorkoutsPerWeek", "IsActive", "SplitTypeId") VALUES (1, 0, true, 1)""")]
    public void AthleteValuesStayWithinTheirDomain(string sql)
    {
        using var app = new TestApplication();
        AddUsers(app);

        Assert.Equal(PostgresErrorCodes.CheckViolation, Refused(app, sql).SqlState);
    }
}
