using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class VariantSchema
{
    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProductVariants" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "ProductId" INTEGER NOT NULL REFERENCES "Products" ("Id") ON DELETE CASCADE,
                "Version" INTEGER NOT NULL DEFAULT 0,
                "Sku" TEXT COLLATE NOCASE NOT NULL,
                "Name" TEXT NOT NULL,
                "Price" TEXT NOT NULL,
                "StockQuantity" INTEGER NULL,
                "IsAvailable" INTEGER NOT NULL,
                "Format" TEXT NOT NULL,
                "Count" INTEGER NULL,
                "Color" TEXT NOT NULL,
                "Size" TEXT NOT NULL,
                "Flavor" TEXT NOT NULL,
                "Material" TEXT NOT NULL,
                "Strength" TEXT NOT NULL,
                "NetContent" TEXT NULL,
                "NetContentUnit" TEXT NOT NULL,
                "ShippingWeightGrams" TEXT NULL,
                "ImageUrl" TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductVariants_Sku" ON "ProductVariants" ("Sku");
            CREATE INDEX IF NOT EXISTS "IX_ProductVariants_ProductId" ON "ProductVariants" ("ProductId");
            INSERT INTO "ProductVariants"
                ("ProductId", "Sku", "Name", "Price", "IsAvailable", "Format", "Color", "Size", "Flavor", "Material", "Strength", "NetContentUnit", "Version")
            SELECT p."Id", 'VR-' || p."Id" || '-DEFAULT',
                CASE WHEN trim(p."Detail") = '' THEN 'Standard' ELSE p."Detail" END,
                CAST(p."Price" AS TEXT), 1, '', '', '', '', '', '', '', 0
            FROM "Products" p
            WHERE NOT EXISTS (SELECT 1 FROM "ProductVariants" v WHERE v."ProductId" = p."Id");
            """);
        await transaction.CommitAsync();
    }
}
