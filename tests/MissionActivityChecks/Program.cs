using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

if (args.Length == 2 && args[0] == "--import")
{
    var database = Path.GetFullPath(args[1]);
    if (!File.Exists(database)) throw new InvalidOperationException("Import requires an existing database.");
    var importOptions = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = database }.ToString()).Options;
    await using var importDb = new CatalogDbContext(importOptions);
    await MissionActivitySchema.EnsureAsync(importDb);
    await MissionActivitySeed.ApplyAsync(importDb);
    Console.WriteLine($"Activity import complete. Stored articles: {await importDb.MissionActivities.CountAsync()}");
    return;
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options;
await using var db = new CatalogDbContext(options);
// Exercise the upgrade path without recreating an existing database.
await db.Database.ExecuteSqlRawAsync("CREATE TABLE ExistingData (Id INTEGER PRIMARY KEY); INSERT INTO ExistingData VALUES (1);");
await db.Database.ExecuteSqlRawAsync("""
    CREATE TABLE MissionActivities (
        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, ActivityDate TEXT NOT NULL,
        Location TEXT NOT NULL, Summary TEXT NOT NULL, Story TEXT NOT NULL, ImageUrl TEXT NOT NULL,
        ImageAlt TEXT NOT NULL, VideoUrl TEXT NOT NULL, IsPublished INTEGER NOT NULL,
        IsShowcased INTEGER NOT NULL, Version INTEGER NOT NULL
    );
    INSERT INTO MissionActivities VALUES (100, 'Preserved draft', '2025-01-01 00:00:00', '', 'Summary', 'Story', '', '', '', 0, 0, 4);
    """);
await MissionActivitySchema.EnsureAsync(db);
await MissionActivitySchema.EnsureAsync(db);
Check((await db.MissionActivities.SingleAsync(x => x.Id == 100)).Version == 4, "Migration preserves existing article identity and version");
MissionActivity Article(string title, int day, bool published, bool showcased) => new()
{
    Title = title, ActivityDate = new DateTime(2026, 10, day), Location = "Test location",
    Summary = "Test summary", Story = "Test story", IsPublished = published, IsShowcased = showcased
};
db.MissionActivities.AddRange(Article("Older", 1, true, true), Article("Newer", 3, true, true),
    Article("Draft", 5, false, true), Article("Unselected", 4, true, false));
await db.SaveChangesAsync();
var visible = await MissionActivitySchema.Showcase(db).Select(x => x.Title).ToListAsync();
Check(visible.SequenceEqual(new[] { "Newer", "Older" }), "SQLite timeline filters and date ordering");
Check(!await db.MissionActivities.AnyAsync(x => x.Title == "Draft" && x.IsPublished), "Draft excluded from article lookup");
Check(await db.MissionActivities.AnyAsync(x => x.Title == "Unselected" && x.IsPublished), "Unselected published article remains accessible");
await using (var command = connection.CreateCommand())
{
    command.CommandText = "SELECT COUNT(*) FROM ExistingData";
    Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 1, "Schema upgrade preserves existing data");
}
var article = Article("Validation", 1, false, false);
bool Valid() => Validator.TryValidateObject(article, new ValidationContext(article), new List<ValidationResult>(), true);
article.VideoUrl = "javascript:alert(1)"; Check(!Valid(), "Reject unsafe video schemes");
article.VideoUrl = "https://youtube.com.evil.example/watch?v=x"; Check(!Valid(), "Reject lookalike video host");
article.VideoUrl = "https://www.youtube.com/watch?v=example"; Check(Valid(), "Accept YouTube link");
article.VideoUrl = "https://vimeo.com/123"; Check(Valid(), "Accept Vimeo link");
article.ImageUrl = "/uploads/products/test.png"; Check(!Valid(), "Photo requires alternative text");
article.ImageAlt = "Community activity"; Check(Valid(), "Photo description accepted");
await using var first = new CatalogDbContext(options);
await using var second = new CatalogDbContext(options);
var a = await first.MissionActivities.FirstAsync();
var b = await second.MissionActivities.FirstAsync();
a.Title = "First edit"; a.Version++; await first.SaveChangesAsync();
b.Title = "Stale edit"; b.Version++;
try { await second.SaveChangesAsync(); throw new Exception("Stale edit was accepted"); }
catch (DbUpdateConcurrencyException) { Console.WriteLine("PASS: Stale edits rejected"); }
await MissionActivitySeed.ApplyAsync(db);
Check(await db.MissionActivities.CountAsync(x => x.SourceNote != "") == 12, "Import stores all twelve source-backed articles");
var seeded = await db.MissionActivities.SingleAsync(x => x.Title == "Eid feeding and gift distribution");
seeded.Title = "Edited by admin"; seeded.IsPublished = false; seeded.Version++;
await db.SaveChangesAsync();
await MissionActivitySeed.ApplyAsync(db);
Check(await db.MissionActivities.CountAsync(x => x.SourceNote != "") == 12, "Repeat import does not duplicate articles");
Check(!seeded.IsPublished && seeded.Title == "Edited by admin", "Repeat import preserves editorial changes");
var iqra = await db.MissionActivities.SingleAsync(x => x.Title == "Donation to IQRA International School");
Check(iqra.ActivityDate == new DateTime(2026, 4, 28), "IQRA uses explicit certificate date");
var dedeiman = await db.MissionActivities.SingleAsync(x => x.Title == "Rashias Foundation community support in Dedeiman");
Check(dedeiman.ActivityDate == new DateTime(2022, 12, 31) && dedeiman.Story.Contains("Rashias Foundation as the donor"), "Dedeiman preserves plaque date and historical attribution");
Check(await db.MissionActivities.CountAsync(x => x.SourceNote != "" && x.ActivityDate == null) == 9, "Unconfirmed dates remain null");
var tank = await db.MissionActivities.SingleAsync(x => x.Title == "Water-access support for Ashifla Otaten");
Check(tank.ActivityDate == new DateTime(2023, 6, 3) && tank.Location.StartsWith("Ashifla Otaten"), "Clearer water-point plaque supplies date and location");
Check(await db.MissionActivities.CountAsync(x => x.IsPledged) == 1, "Borehole remains a pledge");
foreach (var item in await db.MissionActivities.Where(x => x.SourceNote != "").ToListAsync())
    Check(Validator.TryValidateObject(item, new ValidationContext(item), new List<ValidationResult>(), true), $"Imported article validates: {item.Title}");
var ordered = await MissionActivitySchema.Showcase(db).ToListAsync();
Check(ordered.FindLastIndex(x => x.ActivityDate.HasValue) < ordered.FindIndex(x => !x.ActivityDate.HasValue), "Undated articles follow dated entries");
Console.WriteLine("All mission activity checks passed.");
void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
