using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class LocationSchema
{
    public static IQueryable<Location> Published(CatalogDbContext db) => db.Locations.AsNoTracking()
        .Where(x => x.Status == LocationStatus.Published).OrderBy(x => x.Rank).ThenBy(x => x.Id);

    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Locations" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Locations" PRIMARY KEY AUTOINCREMENT,
                "CenterName" TEXT NOT NULL, "AddressLine1" TEXT NOT NULL, "AddressLine2" TEXT NOT NULL,
                "City" TEXT NOT NULL, "Region" TEXT NOT NULL, "PostalCode" TEXT NOT NULL, "Country" TEXT NOT NULL,
                "Phone" TEXT NULL, "Email" TEXT NULL, "Hours" TEXT NOT NULL,
                "Status" INTEGER NOT NULL, "Rank" INTEGER NOT NULL, "Version" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Locations_Status_Rank" ON "Locations" ("Status", "Rank");
            CREATE TABLE IF NOT EXISTS "LocationMigration" ("Id" INTEGER NOT NULL PRIMARY KEY);
            INSERT INTO "Locations" ("CenterName", "AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", "Phone", "Email", "Hours", "Status", "Rank", "Version")
                SELECT "CenterName", "AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", "Phone", "Email", "Hours", 1, 10, 0
                FROM "Headquarters" WHERE "Id" = 1 AND NOT EXISTS (SELECT 1 FROM "LocationMigration");
            INSERT INTO "Locations" ("CenterName", "AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", "Phone", "Email", "Hours", "Status", "Rank", "Version")
                SELECT 'VitalReach US branch', "AddressLine1", "AddressLine2", "City", "Region", "PostalCode", "Country", NULL, NULL, '', 1, 20, 0
                FROM "UsBranches" WHERE "Id" = 1 AND NOT EXISTS (SELECT 1 FROM "LocationMigration");
            INSERT OR IGNORE INTO "LocationMigration" ("Id") VALUES (1);
            """);
        await transaction.CommitAsync();
    }
}
