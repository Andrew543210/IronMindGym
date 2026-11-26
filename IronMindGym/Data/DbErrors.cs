using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IronMindGym.Data;

public static class DbErrors
{
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
