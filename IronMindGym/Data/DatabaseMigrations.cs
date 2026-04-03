using Microsoft.EntityFrameworkCore;
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
    public static void Apply(IronMindDbContext db)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists())
            creator.Create();
        db.GetService<IHistoryRepository>().CreateIfNotExists();
        db.GetService<IMigrator>().Migrate();
    }
}
