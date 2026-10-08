using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class UsBranchSettings
{
    public int Id { get; set; } = 1;
    [Required, MaxLength(160)] public string AddressLine1 { get; set; } = "27 Gwynnswood Rd";
    [MaxLength(160)] public string AddressLine2 { get; set; } = "";
    [Required, MaxLength(100)] public string City { get; set; } = "Owings Mills";
    [Required, MaxLength(100)] public string Region { get; set; } = "MD";
    [Required, MaxLength(30)] public string PostalCode { get; set; } = "21117";
    [Required, MaxLength(100)] public string Country { get; set; } = "United States";
    public string CityRegionPostal => $"{City}, {Region} {PostalCode}";
    private string MapQuery => Uri.EscapeDataString(string.Join(", ", new[] { AddressLine1, AddressLine2, City, Region, PostalCode, Country }.Where(x => !string.IsNullOrWhiteSpace(x))));
    public string MapUrl => $"https://www.google.com/maps/search/?api=1&query={MapQuery}";
    public string MapEmbedUrl => $"https://www.google.com/maps?q={MapQuery}&output=embed";

    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "UsBranches" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UsBranches" PRIMARY KEY,
                "AddressLine1" TEXT NOT NULL, "AddressLine2" TEXT NOT NULL,
                "City" TEXT NOT NULL, "Region" TEXT NOT NULL,
                "PostalCode" TEXT NOT NULL, "Country" TEXT NOT NULL
            );
            """);
        if (!await db.UsBranches.AnyAsync(x => x.Id == 1))
        {
            db.UsBranches.Add(new UsBranchSettings());
            await db.SaveChangesAsync();
        }
    }
}
