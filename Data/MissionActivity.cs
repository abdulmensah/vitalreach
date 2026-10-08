using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class MissionActivity : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Title { get; set; } = "";
    public DateTime? ActivityDate { get; set; }
    [StringLength(120)] public string Location { get; set; } = "";
    public bool IsPledged { get; set; }
    [StringLength(2000)] public string SourceNote { get; set; } = "";
    [Required, StringLength(400)] public string Summary { get; set; } = "";
    [Required, StringLength(20000)] public string Story { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    [StringLength(250)] public string ImageAlt { get; set; } = "";
    [StringLength(500)] public string VideoUrl { get; set; } = "";
    public bool IsPublished { get; set; }
    public bool IsShowcased { get; set; } = true;
    public int Version { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ImageUrl.Length > 0 && string.IsNullOrWhiteSpace(ImageAlt))
            yield return new("Describe the photo for visitors using screen readers.", [nameof(ImageAlt)]);
        if (VideoUrl.Length > 0 && (!Uri.TryCreate(VideoUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)
            || !(new[] { "youtube.com", "www.youtube.com", "youtu.be", "vimeo.com", "www.vimeo.com" }).Contains(uri.Host)))
            yield return new("Use an HTTPS YouTube or Vimeo video link.", [nameof(VideoUrl)]);
    }
}

public static class MissionActivitySchema
{
    public static IQueryable<MissionActivity> Showcase(CatalogDbContext db) => db.MissionActivities.AsNoTracking()
        .Where(x => x.IsPublished && x.IsShowcased).OrderByDescending(x => x.ActivityDate).ThenByDescending(x => x.Id);

    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "MissionActivities" (
            "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            "Title" TEXT NOT NULL, "ActivityDate" TEXT NULL, "Location" TEXT NOT NULL,
            "Summary" TEXT NOT NULL, "Story" TEXT NOT NULL, "ImageUrl" TEXT NOT NULL,
            "ImageAlt" TEXT NOT NULL, "VideoUrl" TEXT NOT NULL,
            "IsPublished" INTEGER NOT NULL, "IsShowcased" INTEGER NOT NULL, "Version" INTEGER NOT NULL,
            "IsPledged" INTEGER NOT NULL DEFAULT 0, "SourceNote" TEXT NOT NULL DEFAULT ''
        );
        """);
        var connection = db.Database.GetDbConnection();
        var close = connection.State != System.Data.ConnectionState.Open;
        if (close) await connection.OpenAsync();
        var columns = new HashSet<string>();
        bool requiredDate = false;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info('MissionActivities')";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
                if (reader.GetString(1) == "ActivityDate") requiredDate = reader.GetInt32(3) == 1;
            }
        }
        finally { if (close) await connection.CloseAsync(); }
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!columns.Contains("IsPledged")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE MissionActivities ADD COLUMN IsPledged INTEGER NOT NULL DEFAULT 0");
        if (!columns.Contains("SourceNote")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE MissionActivities ADD COLUMN SourceNote TEXT NOT NULL DEFAULT ''");
        if (requiredDate)
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE MissionActivities_Upgrade (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL,
                    ActivityDate TEXT NULL, Location TEXT NOT NULL, Summary TEXT NOT NULL,
                    Story TEXT NOT NULL, ImageUrl TEXT NOT NULL, ImageAlt TEXT NOT NULL,
                    VideoUrl TEXT NOT NULL, IsPublished INTEGER NOT NULL, IsShowcased INTEGER NOT NULL,
                    Version INTEGER NOT NULL, IsPledged INTEGER NOT NULL DEFAULT 0, SourceNote TEXT NOT NULL DEFAULT ''
                );
                INSERT INTO MissionActivities_Upgrade
                    (Id, Title, ActivityDate, Location, Summary, Story, ImageUrl, ImageAlt, VideoUrl, IsPublished, IsShowcased, Version, IsPledged, SourceNote)
                    SELECT Id, Title, ActivityDate, Location, Summary, Story, ImageUrl, ImageAlt, VideoUrl, IsPublished, IsShowcased, Version, IsPledged, SourceNote FROM MissionActivities;
                DROP TABLE MissionActivities;
                ALTER TABLE MissionActivities_Upgrade RENAME TO MissionActivities;
                """);
        }
        await transaction.CommitAsync();
    }
}
