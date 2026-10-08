using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public enum SocialPlatform { LinkedIn, Instagram, Facebook, TikTok, YouTube, X, Other }

public sealed class SocialLink : IValidatableObject
{
    public int Id { get; set; }
    [EnumDataType(typeof(SocialPlatform))] public SocialPlatform Platform { get; set; } = SocialPlatform.TikTok;
    [Required, MaxLength(80)] public string Label { get; set; } = "TikTok";
    [Required, MaxLength(500)] public string Url { get; set; } = "";
    public bool IsPublished { get; set; }
    [Range(0, 100000)] public int Rank { get; set; } = 100;
    public int Version { get; set; }

    public static bool IsValidUrl(SocialPlatform platform, string? value)
    {
        if (!Enum.IsDefined(platform) || !Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0) return false;
        var domain = platform switch {
            SocialPlatform.LinkedIn => "linkedin.com", SocialPlatform.Instagram => "instagram.com",
            SocialPlatform.Facebook => "facebook.com", SocialPlatform.TikTok => "tiktok.com",
            SocialPlatform.YouTube => "youtube.com", SocialPlatform.X => "x.com", _ => null
        };
        return domain is null || SocialSettings.IsValidProfile(value, domain);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsValidUrl(Platform, Url)) yield return new ValidationResult("Enter an HTTPS profile URL for the selected platform (or choose Other for another network).", [nameof(Url)]);
    }

    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SocialLinks" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Platform" INTEGER NOT NULL, "Label" TEXT NOT NULL, "Url" TEXT NOT NULL,
                "IsPublished" INTEGER NOT NULL, "Rank" INTEGER NOT NULL, "Version" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "SocialLinkMigration" ("Id" INTEGER NOT NULL PRIMARY KEY);
            INSERT INTO "SocialLinks" ("Platform", "Label", "Url", "IsPublished", "Rank", "Version")
                SELECT 0, 'LinkedIn', "LinkedInUrl", 1, 10, 0 FROM "SocialSettings"
                WHERE "Id" = 1 AND trim("LinkedInUrl") <> '' AND NOT EXISTS (SELECT 1 FROM "SocialLinkMigration");
            INSERT INTO "SocialLinks" ("Platform", "Label", "Url", "IsPublished", "Rank", "Version")
                SELECT 1, 'Instagram', "InstagramUrl", 1, 20, 0 FROM "SocialSettings"
                WHERE "Id" = 1 AND trim("InstagramUrl") <> '' AND NOT EXISTS (SELECT 1 FROM "SocialLinkMigration");
            INSERT INTO "SocialLinks" ("Platform", "Label", "Url", "IsPublished", "Rank", "Version")
                SELECT 2, 'Facebook', "FacebookUrl", 1, 30, 0 FROM "SocialSettings"
                WHERE "Id" = 1 AND trim("FacebookUrl") <> '' AND NOT EXISTS (SELECT 1 FROM "SocialLinkMigration");
            INSERT OR IGNORE INTO "SocialLinkMigration" ("Id") VALUES (1);
            """);
        await transaction.CommitAsync();
    }
}
