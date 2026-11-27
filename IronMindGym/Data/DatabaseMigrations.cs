using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace IronMindGym.Data;

public static class DatabaseMigrations
{
    /// <summary>
    /// Applies every pending migration. Migrate() would create the database and the history table anyway;
    /// doing it first keeps the PostgreSQL provider from logging the missing history table of a new database as an error.
    /// </summary>
    public static async Task ApplyAsync(IronMindDbContext db, CancellationToken cancellationToken)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync(cancellationToken))
            await creator.CreateAsync(cancellationToken);
        await db.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken);
        await db.GetService<IMigrator>().MigrateAsync(cancellationToken: cancellationToken);
    }
}
