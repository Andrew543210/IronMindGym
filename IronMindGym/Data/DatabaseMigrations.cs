using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace IronMindGym.Data;

public static class DatabaseMigrations
{
    // Creating the database first keeps the provider from logging its missing history table as an error.
    public static async Task ApplyAsync(IronMindDbContext db, CancellationToken cancellationToken)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync(cancellationToken))
            await creator.CreateAsync(cancellationToken);
        await db.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken);
        await db.GetService<IMigrator>().MigrateAsync(cancellationToken: cancellationToken);
    }
}
