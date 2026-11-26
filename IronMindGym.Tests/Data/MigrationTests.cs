using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

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

    [Theory]
    [InlineData(2, 1, 2, "Pending")]
    [InlineData(1, 2, 3, "Pending")]
    [InlineData(1, 2, 1, "Blocked")]
    public void TheDatabaseRefusesAFriendshipThatBreaksTheRules(int user1, int user2, int requester, string status)
    {
        MigrateTo("EnforceFriendshipRules");
        AddUsers(1, 2, 3);

        var refused = Assert.Throws<PostgresException>(() => Run($"""
            INSERT INTO "Friendships" ("User1Id", "User2Id", "RequestedByUserId", "Status") VALUES ({user1}, {user2}, {requester}, '{status}')
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, refused.SqlState);
    }

    public void Dispose() => TestDatabase.Drop(connectionString);
}
