using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class SocialSettings
{
    public int Id { get; set; } = 1;
    [MaxLength(500), SocialProfile("linkedin.com")]
    public string LinkedInUrl { get; set; } = "https://www.linkedin.com/company/vitalreachhub/";
    [MaxLength(500), SocialProfile("instagram.com")]
    public string InstagramUrl { get; set; } = "https://www.instagram.com/vitalreachwellnesshub/";
    [MaxLength(500), SocialProfile("facebook.com")]
    public string FacebookUrl { get; set; } = "https://www.facebook.com/VitalReachWellnessHub";

    public static bool IsValidProfile(string? value, string domain) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("www." + domain, StringComparison.OrdinalIgnoreCase))
        && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo)
        && uri.AbsolutePath.Trim('/').Length > 0;

    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SocialSettings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SocialSettings" PRIMARY KEY,
                "LinkedInUrl" TEXT NOT NULL,
                "InstagramUrl" TEXT NOT NULL,
                "FacebookUrl" TEXT NOT NULL
            );
            """);
        if (!await db.SocialSettings.AnyAsync(x => x.Id == 1))
        {
            db.SocialSettings.Add(new SocialSettings());
            await db.SaveChangesAsync();
        }
    }
}

public sealed class SocialProfileAttribute(string domain) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null
        || value is string text && (string.IsNullOrWhiteSpace(text) || SocialSettings.IsValidProfile(text, domain));

    public override string FormatErrorMessage(string name) =>
        $"{name} must be an HTTPS profile URL on {domain}, or blank to hide the icon.";
}
