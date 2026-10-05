using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using VitalReach.Web.Components;
using VitalReach.Web.Components.Pages;
using VitalReach.Web.Data;

int checks = 0;
void Check(bool ok, string description) { if (!ok) throw new Exception(description); checks++; }
using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var services = new ServiceCollection().AddLogging().AddDbContextFactory<CatalogDbContext>(o => o.UseSqlite(connection)).BuildServiceProvider();
var factory = services.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
await using (var db = await factory.CreateDbContextAsync())
{
    await db.Database.EnsureCreatedAsync();
    db.Headquarters.Add(new HeadquartersSettings { City = "Existing city" });
    await db.SaveChangesAsync();
    // Simulate an existing database from before these settings were introduced.
    await db.Database.ExecuteSqlRawAsync("DROP TABLE SocialSettings; DROP TABLE UsBranches;");
    await SocialSettings.EnsureAsync(db);
    await UsBranchSettings.EnsureAsync(db);
    Check((await db.Headquarters.SingleAsync()).City == "Existing city", "upgrade preserves existing headquarters");
    var social = await db.SocialSettings.SingleAsync();
    Check(social.LinkedInUrl.Contains("vitalreachhub"), "upgrade supplies initial social profiles");
    social.LinkedInUrl = "https://www.linkedin.com/company/updated-profile/";
    social.InstagramUrl = "";
    var branch = await db.UsBranches.SingleAsync();
    branch.AddressLine1 = "10 Test & Example Road";
    branch.City = "Updated city";
    await db.SaveChangesAsync();
}
await using (var db = await factory.CreateDbContextAsync())
{
    await SocialSettings.EnsureAsync(db);
    await UsBranchSettings.EnsureAsync(db);
    Check((await db.SocialSettings.SingleAsync()).InstagramUrl == "", "restart preserves hidden social icon");
    Check((await db.UsBranches.SingleAsync()).City == "Updated city", "restart preserves edited address");
}
foreach (var value in new[] { "javascript:alert(1)", "http://facebook.com/test", "https://facebook.com.evil.test/profile", "https://facebook.com@evil.test/profile" })
    Check(!SocialSettings.IsValidProfile(value, "facebook.com"), "unsafe or wrong-host URL rejected");
Check(SocialSettings.IsValidProfile("https://www.facebook.com/profile.php?id=123", "facebook.com"), "Facebook query-style profile supported");
var blank = new SocialSettings { InstagramUrl = "" };
Check(Validator.TryValidateObject(blank, new ValidationContext(blank), new List<ValidationResult>(), true), "blank social URL allowed");
await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
async Task<string> Render<T>() where T : IComponent => await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<T>()).ToHtmlString());
await using (var db = await factory.CreateDbContextAsync()) { await SocialLink.EnsureAsync(db); }
var icons = await Render<SocialLinks>();
Check(icons.Contains("updated-profile") && !icons.Contains("title=\"Instagram\""), "footer icons render saved URL and hide blank profile");
await using (var db = await factory.CreateDbContextAsync()) { await LocationSchema.EnsureAsync(db); }
var html = await Render<BranchLocations>();
Check(html.Contains("Updated city") && html.Contains("10%20Test%20%26%20Example%20Road"), "branch renders saved address and encoded map query");
var footer = await Render<StorefrontFooter>();
Check(footer.Contains("Updated city") && !footer.Contains("27 Gwynnswood"), "footer uses saved branch address");
await using (var db = await factory.CreateDbContextAsync())
{
    Check(await db.Locations.CountAsync() == 2, "existing addresses migrate once");
    var original = await db.Locations.SingleAsync(x => x.Rank == 10);
    Check(original.City == "Existing city", "migration preserves edited headquarters");
    original.Status = LocationStatus.Archived;
    db.Locations.Add(new Location { CenterName = "Hidden draft", AddressLine1 = "Draft address", City = "Test", Country = "Test", Rank = 0 });
    db.Locations.Add(new Location { CenterName = "New first branch", AddressLine1 = "Published address", City = "Test", Country = "Test", Status = LocationStatus.Published, Rank = 5 });
    db.Locations.Add(new Location { CenterName = "Tied branch", AddressLine1 = "Tie address", City = "Test", Country = "Test", Status = LocationStatus.Published, Rank = 5 });
    await db.SaveChangesAsync();
    await LocationSchema.EnsureAsync(db);
    Check(await db.Locations.CountAsync() == 5, "migration never duplicates or republishes existing locations");
    var published = await LocationSchema.Published(db).ToListAsync();
    Check(published.Select(x => x.CenterName).SequenceEqual(new[] { "New first branch", "Tied branch", "VitalReach US branch" }), "only published locations appear, ranked with stable tie order");
}
footer = await Render<StorefrontFooter>();
Check(!footer.Contains("Existing city") && !footer.Contains("Hidden draft"), "footer omits archived and draft locations");
Check(footer.IndexOf("New first branch") < footer.IndexOf("VitalReach US branch"), "footer follows rank");
html = await Render<BranchLocations>();
Check(!html.Contains("Existing city") && !html.Contains("Draft address") && html.Contains("Published%20address"), "maps omit unpublished locations and include new branch");
await using (var db = await factory.CreateDbContextAsync())
{
    await db.Locations.ExecuteUpdateAsync(x => x.SetProperty(l => l.Status, LocationStatus.Archived));
    await LocationSchema.EnsureAsync(db);
    Check(!await LocationSchema.Published(db).AnyAsync(), "all locations can remain unpublished after restart");
}
Check(!(await Render<StorefrontFooter>()).Contains("<address"), "footer supports zero published locations");
Check(string.IsNullOrWhiteSpace(await Render<BranchLocations>()), "map list supports zero published locations");
const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
AdminHeadquarters Admin()
{
    var page = new AdminHeadquarters();
    typeof(AdminHeadquarters).GetProperty("DbFactory", flags)!.SetValue(page, factory);
    typeof(AdminHeadquarters).GetProperty("Confirmation", flags)!.SetValue(page, new AdminConfirmation(new AcceptRuntime()));
    return page;
}
Task Invoke(AdminHeadquarters page, string method, params object[] args) => (Task)typeof(AdminHeadquarters).GetMethod(method, flags)!.Invoke(page, args)!;
Location Draft(AdminHeadquarters page) => (Location)typeof(AdminHeadquarters).GetField("Draft", flags)!.GetValue(page)!;
var admin = Admin();
await Invoke(admin, "NewAsync");
var draft = Draft(admin);
draft.CenterName = "Admin-created branch"; draft.AddressLine1 = "123 Admin Road"; draft.City = "Test"; draft.Country = "Test";
await Invoke(admin, "SaveAsync");
Location saved;
await using (var db = await factory.CreateDbContextAsync())
{
    saved = await db.Locations.AsNoTracking().SingleAsync(x => x.CenterName == "Admin-created branch");
    Check(saved.Status == LocationStatus.Draft, "admin creates a draft by default");
}
var other = Admin();
await Invoke(admin, "EditAsync", saved);
await Invoke(other, "EditAsync", saved);
Draft(admin).Status = LocationStatus.Published; Draft(admin).Rank = 1;
await Invoke(admin, "SaveAsync");
Draft(other).CenterName = "Stale update";
await Invoke(other, "SaveAsync");
await using (var db = await factory.CreateDbContextAsync())
{
    var result = await db.Locations.SingleAsync(x => x.Id == saved.Id);
    Check(result.Status == LocationStatus.Published && result.Rank == 1, "admin saves publication and rank");
    Check(result.CenterName == "Admin-created branch", "stale admin edit cannot overwrite newer changes");
}
var socialAdmin = new AdminSocial();
typeof(AdminSocial).GetProperty("DbFactory", flags)!.SetValue(socialAdmin, factory);
typeof(AdminSocial).GetProperty("Confirmation", flags)!.SetValue(socialAdmin, new AdminConfirmation(new AcceptRuntime()));
await (Task)typeof(AdminSocial).GetMethod("NewAsync", flags)!.Invoke(socialAdmin, null)!;
var socialDraft = (SocialLink)typeof(AdminSocial).GetField("Draft", flags)!.GetValue(socialAdmin)!;
Check(!socialDraft.IsPublished, "new social profiles start hidden");
socialDraft.Url = "https://www.tiktok.com/@test-account";
socialDraft.IsPublished = true;
socialDraft.Rank = 1;
await (Task)typeof(AdminSocial).GetMethod("SaveAsync", flags)!.Invoke(socialAdmin, null)!;
await using (var db = await factory.CreateDbContextAsync())
{
    await SocialLink.EnsureAsync(db);
    Check(await db.SocialLinks.CountAsync() == 3, "social migration preserves blank profiles and does not duplicate after adding TikTok");
    Check(await db.SocialLinks.AnyAsync(x => x.Platform == SocialPlatform.TikTok && x.IsPublished), "admin saves TikTok profile");
}
icons = await Render<SocialLinks>();
Check(icons.Contains("tiktok.com/@test-account") && icons.IndexOf("TikTok") < icons.IndexOf("LinkedIn"), "TikTok renders in ranked footer order");
Check(!SocialLink.IsValidUrl(SocialPlatform.TikTok, "https://tiktok.com.evil.test/@test"), "TikTok spoofed domain rejected");
Check(!SocialLink.IsValidUrl(SocialPlatform.Other, "javascript:alert(1)"), "custom platform rejects unsafe schemes");
Check(SocialLink.IsValidUrl(SocialPlatform.Other, "https://social.example/profile"), "other supports additional platforms");
await using (var db = await factory.CreateDbContextAsync())
{
    var tiktok = await db.SocialLinks.SingleAsync(x => x.Platform == SocialPlatform.TikTok);
    tiktok.IsPublished = false;
    await db.SaveChangesAsync();
    await SocialLink.EnsureAsync(db);
}
Check(!(await Render<SocialLinks>()).Contains("TikTok"), "hidden TikTok stays hidden across restart");
Console.WriteLine($"PASS: {checks} site settings checks.");

sealed class AcceptRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult((TValue)(object)true);
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
}
