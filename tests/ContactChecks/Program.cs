using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Components.Pages;
using VitalReach.Web.Data;

var file = Path.Combine(Path.GetTempPath(), $"vitalreach-contacts-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite($"Data Source={file};Pooling=False").Options;
const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
var page = new AdminContacts();
typeof(AdminContacts).GetProperty("DbFactory", flags)!.SetValue(page, new Factory(options));
Task Invoke(string name, params object[] arguments) => (Task)typeof(AdminContacts).GetMethod(name, flags)!.Invoke(page, arguments)!;
List<ContactSubmission> Messages() => (List<ContactSubmission>)typeof(AdminContacts).GetField("Submissions", flags)!.GetValue(page)!;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
}
try
{
    await using (var db = new CatalogDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        db.ContactSubmissions.AddRange(
            new() { Name = "Older unread", Email = "test@example.com", Phone = "2025550123", Message = "Test message one", CreatedUtc = DateTimeOffset.Parse("2026-09-23T10:00:00+00:00") },
            new() { Name = "Newest read", Email = "test@example.com", Phone = "2025550123", Message = "Test message two", IsRead = true, CreatedUtc = DateTimeOffset.Parse("2026-09-23T15:00:00+00:00") },
            new() { Name = "Newer unread", Email = "test@example.com", Phone = "2025550123", Message = "Test message three", CreatedUtc = DateTimeOffset.Parse("2026-09-23T08:00:00-04:00") });
        await db.SaveChangesAsync();
    }
    await Invoke("LoadAsync");
    Check(Messages().Select(x => x.Name).SequenceEqual(["Newer unread", "Older unread", "Newest read"]), "page loads SQLite messages with unread first and true chronological ordering across offsets");
    var selected = Messages()[0];
    await Invoke("ToggleReadAsync", selected);
    Check(Messages().Select(x => x.Name).SequenceEqual(["Older unread", "Newest read", "Newer unread"]), "mark read saves and reloads in the correct order");
    await Invoke("ToggleReadAsync", Messages().Single(x => x.Id == selected.Id));
    Check(Messages()[0].Id == selected.Id && !Messages()[0].IsRead, "mark unread saves and reloads");
    await Invoke("DeleteAsync", Messages()[0]);
    Check(Messages().Count == 2 && Messages().All(x => x.Id != selected.Id), "delete saves and reloads remaining messages");
    foreach (var item in Messages().ToList()) await Invoke("DeleteAsync", item);
    Check(Messages().Count == 0, "empty inbox loads successfully");
}
finally { File.Delete(file); }

sealed class Factory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext() => new(options);
}
