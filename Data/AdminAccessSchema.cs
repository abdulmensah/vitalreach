using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class AdminAccessSchema
{
    public static async Task EnsureAsync(CatalogDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(AdminUsers);";
            var columns = new HashSet<string>();
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
            if (!columns.Contains("IsClinicalReviewer")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE AdminUsers ADD COLUMN IsClinicalReviewer INTEGER NOT NULL DEFAULT 0;");
            if (!columns.Contains("IsSuperAdmin")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE AdminUsers ADD COLUMN IsSuperAdmin INTEGER NOT NULL DEFAULT 0;");
        }
        finally { await connection.CloseAsync(); }
    }

    // One-time migration requested by the owner. Subsequent revocations must survive every restart.
    public static Task BootstrapAsync(CatalogDbContext db) => db.Database.ExecuteSqlRawAsync("""
        UPDATE AdminUsers SET IsClinicalReviewer = 1
        WHERE NormalizedEmail IN ('ABDULMENSAH@GMAIL.COM', 'MASAOUDAA@GMAIL.COM')
          AND NOT EXISTS (SELECT 1 FROM SeedHistory WHERE Key = 'clinical-access-v1');
        UPDATE AdminUsers SET IsSuperAdmin = 1
        WHERE NormalizedEmail = 'ABDULMENSAH@GMAIL.COM'
          AND NOT EXISTS (SELECT 1 FROM SeedHistory WHERE Key = 'clinical-access-v1');
        INSERT OR IGNORE INTO SeedHistory (Key, AppliedUtc) VALUES ('clinical-access-v1', datetime('now'));
        """);
}
