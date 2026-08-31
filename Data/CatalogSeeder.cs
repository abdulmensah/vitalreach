using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        await EnsureProductColumnsAsync(db);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AdminUsers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AdminUsers" PRIMARY KEY AUTOINCREMENT,
                "Email" TEXT NOT NULL,
                "NormalizedEmail" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedUtc" TEXT NOT NULL,
                "CreatedBy" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL,
                "UpdatedBy" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AdminUsers_NormalizedEmail" ON "AdminUsers" ("NormalizedEmail");
            CREATE TABLE IF NOT EXISTS "Headquarters" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Headquarters" PRIMARY KEY,
                "CenterName" TEXT NOT NULL,
                "AddressLine1" TEXT NOT NULL,
                "AddressLine2" TEXT NOT NULL,
                "City" TEXT NOT NULL,
                "Region" TEXT NOT NULL,
                "PostalCode" TEXT NOT NULL,
                "Country" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Hours" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "ContactSubmissions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ContactSubmissions" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "IsRead" INTEGER NOT NULL,
                "CreatedUtc" TEXT NOT NULL
            );
            """);
        if (!await db.Products.AnyAsync()) db.Products.AddRange(
            New("energy", "Ultra Energy Shot™", 24m, "Founder's Collection", "Caffeine-free focus & vitality", "30 servings", "gold-product", "30 mL", "Ultra Energy", "Shot", 10),
            New("magnesium", "Magnesium Glycinate Complex+", 29m, "Daily Wellness", "Everyday calm & muscle support", "60 capsules", "teal-product", "COMPLEX+", "Magnesium", "Complex+", 20),
            New("collagen", "Marine Collagen Glow", 38m, "Women's Beauty", "Beauty, hydration & glow support", "30 servings", "pearl-product", "GLOW", "Marine Collagen", "Glow", 30),
            New("beetroot", "Beetroot Plus", 27m, "Active Living", "Circulation, endurance & stamina", "60 capsules", "berry-product", "500 mg", "Beetroot", "Plus", 40));
        if (!await db.AdminUsers.AnyAsync()) db.AdminUsers.AddRange(
            AdminUser.Create("abdulmensah@gmail.com", "Abdul Mensah", "system-seed"),
            AdminUser.Create("masaoudaa@gmail.com", "Masaouda", "system-seed"));
        if (!await db.Headquarters.AnyAsync()) db.Headquarters.Add(new HeadquartersSettings());
        await db.SaveChangesAsync();
    }

    private static async Task EnsureProductColumnsAsync(CatalogDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var columns = connection.CreateCommand();
            columns.CommandText = "PRAGMA table_info(\"Products\");";
            await using var reader = await columns.ExecuteReaderAsync();
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));

            await reader.DisposeAsync();
            if (!existingColumns.Contains(nameof(ProductEntity.ImageUrl)))
                await AddColumnAsync(connection, "ALTER TABLE \"Products\" ADD COLUMN \"ImageUrl\" TEXT NULL;");
            if (!existingColumns.Contains(nameof(ProductEntity.Description)))
                await AddColumnAsync(connection, "ALTER TABLE \"Products\" ADD COLUMN \"Description\" TEXT NOT NULL DEFAULT '';");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task AddColumnAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var alter = connection.CreateCommand();
        alter.CommandText = sql;
        await alter.ExecuteNonQueryAsync();
    }

    private static ProductEntity New(string slug, string name, decimal price, string category, string benefit, string detail, string theme, string orb, string one, string two, int order) => new() { Slug=slug, Name=name, Price=price, Category=category, Benefit=benefit, Detail=detail, Theme=theme, Orb=orb, LabelOne=one, LabelTwo=two, SortOrder=order };
}
