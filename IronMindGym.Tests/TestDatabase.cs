using IronMindGym.Data;
using IronMindGym.Seed;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IronMindGym.Tests;

/// <summary>
/// One PostgreSQL server for the whole test run, started in Docker by Testcontainers.
/// Every test application gets its own database copied from a migrated and seeded template,
/// so tests never share data or touch a working database.
/// Set IRONMIND_TEST_POSTGRES to a server connection string to use an existing server instead of Docker.
/// </summary>
internal static class TestDatabase
{
    private const string TemplateName = "ironmind_test_template";
    private static readonly Lazy<string> Server = new(StartServerWithTemplate, LazyThreadSafetyMode.ExecutionAndPublication);
    // PostgreSQL refuses to copy a template while another copy of it is being made.
    private static readonly object CopyLock = new();

    public static string Create()
    {
        var name = $"ironmind_test_{Guid.NewGuid():N}";
        lock (CopyLock)
            Execute(Server.Value, $"CREATE DATABASE \"{name}\" TEMPLATE \"{TemplateName}\"");
        return WithDatabase(Server.Value, name);
    }

    public static void Drop(string connectionString) =>
        Execute(Server.Value, $"DROP DATABASE IF EXISTS \"{new NpgsqlConnectionStringBuilder(connectionString).Database}\" WITH (FORCE)");

    private static string StartServerWithTemplate()
    {
        var server = Environment.GetEnvironmentVariable("IRONMIND_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(server))
        {
            var container = new PostgreSqlBuilder("postgres:18-alpine").Build();
            container.StartAsync().GetAwaiter().GetResult();
            server = WithDatabase(container.GetConnectionString(), "postgres");
        }
        Execute(server, $"DROP DATABASE IF EXISTS \"{TemplateName}\" WITH (FORCE)");
        Execute(server, $"CREATE DATABASE \"{TemplateName}\"");
        var options = new DbContextOptionsBuilder<IronMindDbContext>().UseNpgsql(WithDatabase(server, TemplateName)).Options;
        using (var db = new IronMindDbContext(options))
        {
            DatabaseMigrations.Apply(db);
            DataSeeder.Seed(db);
        }
        return server;
    }

    private static string WithDatabase(string connectionString, string database) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = database, Pooling = false }.ConnectionString;

    private static void Execute(string connectionString, string sql)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
