using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IronMindGym.Tests.Data;

public class SchemaRulesTests
{
    private static PostgresException Refused(TestApplication app, string sql) =>
        app.Query(db => Assert.Throws<PostgresException>(() => db.Database.ExecuteSqlRaw(sql)));

    private static int AddUsers(TestApplication app) => app.Query(db => db.Database.ExecuteSqlRaw("""
        INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "SecurityStamp", "CreatedAt") VALUES
            (1, 'one', 'one@example.com', 'hash', 'stamp', now()),
            (2, 'two', 'two@example.com', 'hash', 'stamp', now()),
            (3, 'three', 'three@example.com', 'hash', 'stamp', now())
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
    [InlineData("""INSERT INTO "Profiles" ("UserId", "Age", "EnergyEquation") VALUES (1, 30, 'Other')""")]
    [InlineData("""INSERT INTO "Profiles" ("UserId", "Age", "EnergyEquation", "Language") VALUES (1, 30, 'None', 'de')""")]
    [InlineData("""INSERT INTO "Profiles" ("UserId", "Age", "EnergyEquation", "CalorieTarget") VALUES (1, 30, 'None', 0)""")]
    [InlineData("""INSERT INTO "WorkoutPlans" ("UserId", "WorkoutsPerWeek", "SplitTypeId") VALUES (1, 0, 1)""")]
    [InlineData("""INSERT INTO "Users" ("Username", "Email", "PasswordHash", "SecurityStamp", "CreatedAt") VALUES (repeat('a', 51), 'long@example.com', 'hash', 'stamp', now())""")]
    [InlineData("""INSERT INTO "Users" ("Username", "Email", "PasswordHash", "SecurityStamp", "CreatedAt") VALUES ('long', repeat('a', 110) || '@example.com', 'hash', 'stamp', now())""")]
    [InlineData("""INSERT INTO "WorkoutSessions" ("UserId", "SessionDate", "DurationMinutes", "Rating") VALUES (1, now(), 40, 6)""")]
    [InlineData("""
        WITH session AS (INSERT INTO "WorkoutSessions" ("UserId", "SessionDate", "DurationMinutes", "Rating") VALUES (1, now(), 40, 4) RETURNING "Id")
        INSERT INTO "PerformedExercises" ("WorkoutSessionId", "ExerciseId", "OrderInWorkout") SELECT "Id", (SELECT min("Id") FROM "Exercises"), 0 FROM session
        """)]
    [InlineData("""UPDATE "Exercises" SET "KeyLift" = 'Snatch' WHERE "Id" = (SELECT min("Id") FROM "Exercises" WHERE "KeyLift" IS NULL)""")]
    public void AthleteValuesStayWithinTheirDomain(string sql)
    {
        using var app = new TestApplication();
        AddUsers(app);

        Assert.Equal(PostgresErrorCodes.CheckViolation, Refused(app, sql).SqlState);
    }
}
