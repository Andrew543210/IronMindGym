using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IronMindGym.Tests.Data;

/// <summary>
/// Migrations that move data are checked against a database that holds rows written under the previous schema.
/// </summary>
public sealed class MigrationTests : IDisposable
{
    private readonly string connectionString = TestDatabase.CreateEmpty();

    private IronMindDbContext Open() =>
        new(new DbContextOptionsBuilder<IronMindDbContext>().UseNpgsql(connectionString).Options);

    private void MigrateTo(string migration)
    {
        using var db = Open();
        db.GetService<IMigrator>().Migrate(db.Database.GetMigrations().Single(m => m.EndsWith($"_{migration}", StringComparison.Ordinal)));
    }

    private void Run(string sql)
    {
        using var db = Open();
        db.Database.ExecuteSqlRaw(sql);
    }

    private T Read<T>(Func<IronMindDbContext, T> query)
    {
        using var db = Open();
        return query(db);
    }

    private void AddUsers(params int[] ids)
    {
        foreach (var id in ids)
        {
            Run($"""
                INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "SecurityStamp")
                VALUES ({id}, 'athlete{id}', 'athlete{id}@example.com', 'hash', 'stamp')
                """);
        }
    }

    [Fact]
    public void FriendshipsWrittenBeforeTheRulesAreRepaired()
    {
        MigrateTo("RestrictReferenceDeletes");
        AddUsers(1, 2, 3);
        Run("""
            INSERT INTO "Friendships" ("User1Id", "User2Id", "RequestedByUserId", "Status") VALUES
                (2, 1, 0, 'Accepted'),
                (3, 1, 3, 'Accepted'),
                (1, 3, 1, 'Pending'),
                (2, 2, 2, 'Pending');
            """);

        MigrateTo("EnforceFriendshipRules");

        // The pair stored both ways keeps its accepted row, and the athlete paired with themselves is gone.
        var rows = Read(db => db.Friendships.OrderBy(f => f.User2Id)
            .Select(f => new { f.User1Id, f.User2Id, f.RequestedByUserId, Accepted = f.Status == FriendshipStatus.Accepted }).ToList());
        Assert.Equal(
            [new { User1Id = 1, User2Id = 2, RequestedByUserId = 1, Accepted = true }, new { User1Id = 1, User2Id = 3, RequestedByUserId = 3, Accepted = true }],
            rows);
    }

    [Fact]
    public void MeasurementsBecomeWeighInsOnTheAthletesDayAndTheLatestHeight()
    {
        MigrateTo("AddCheckConstraints");
        AddUsers(1, 2);
        Run("""
            INSERT INTO "Profiles" ("UserId", "Age", "AccountCreatedAt", "EnergyEquation", "TimeZone") VALUES
                (1, 30, now(), 'None', 'Pacific/Kiritimati'),
                (2, 30, now(), 'None', 'Not/AZone');
            INSERT INTO "WeightLogs" ("UserId", "Date", "ValueKg") VALUES (1, '2026-11-02', 79);
            INSERT INTO "AnthropometryLogs" ("UserId", "WeightKg", "HeightCm", "RecordedAt") VALUES
                (1, 81, 180, '2026-11-01 08:00+00'),
                (1, 80, 181, '2026-11-01 20:00+00'),
                (1, 82, 182, '2026-11-03 09:00+00'),
                (2, 60, 165, '2026-11-01 20:00+00');
            """);

        MigrateTo("KeepBodyweightInWeighIns");

        var weighIns = Read(db => db.WeightLogs.OrderBy(w => w.UserId).ThenBy(w => w.Date).Select(w => new { w.UserId, w.Date, w.ValueKg }).ToList());
        Assert.Equal(
            [
                new { UserId = 1, Date = new DateOnly(2026, 11, 1), ValueKg = 81.0 },
                new { UserId = 1, Date = new DateOnly(2026, 11, 2), ValueKg = 79.0 },
                new { UserId = 1, Date = new DateOnly(2026, 11, 3), ValueKg = 82.0 },
                new { UserId = 2, Date = new DateOnly(2026, 11, 1), ValueKg = 60.0 }
            ],
            weighIns);
        Assert.Equal([182.0, 165.0], Read(db => db.Profiles.OrderBy(p => p.UserId).Select(p => p.HeightCm!.Value).ToList()));
    }

    public void Dispose() => TestDatabase.Drop(connectionString);
}
