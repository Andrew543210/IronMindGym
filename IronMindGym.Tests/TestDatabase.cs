using IronMindGym.Data;
using IronMindGym.Seed;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IronMindGym.Tests;

// One PostgreSQL server per test run; every test application gets its own copy of a migrated template.
// Set IRONMIND_TEST_POSTGRES to a server connection string to use an existing server instead of Docker.
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

    public static string CreateEmpty()
    {
        var name = $"ironmind_test_{Guid.NewGuid():N}";
        Execute(Server.Value, $"CREATE DATABASE \"{name}\"");
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
        // The template is built once, inside a Lazy, so the asynchronous setup is awaited off the test's context.
        Task.Run(async () =>
        {
            await using var db = new IronMindDbContext(options);
            await DatabaseMigrations.ApplyAsync(db, CancellationToken.None);
            await DataSeeder.SeedAsync(db, includeDemoData: false, TimeProvider.System, CancellationToken.None);
        }).GetAwaiter().GetResult();
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
