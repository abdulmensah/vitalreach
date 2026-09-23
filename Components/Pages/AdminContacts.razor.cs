#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminContacts
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;

    private List<ContactSubmission> Submissions = [];
    private string? StatusMessage;
    private bool IsError;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        // SQLite cannot order DateTimeOffset values. Sort after materializing so offsets
        // are compared as actual instants without changing existing stored timestamps.
        var submissions = await db.ContactSubmissions.AsNoTracking().ToListAsync();
        Submissions = submissions.OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedUtc).ThenByDescending(x => x.Id).ToList();
    }

    private async Task ToggleReadAsync(ContactSubmission selected)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var submission = await db.ContactSubmissions.FindAsync(selected.Id);
        if (submission is null) return;
        submission.IsRead = !submission.IsRead;
        await db.SaveChangesAsync();
        IsError = false;
        StatusMessage = $"Message from {submission.Name} has been marked as {(submission.IsRead ? "read" : "unread")} successfully.";
        await LoadAsync();
    }

    private async Task DeleteAsync(ContactSubmission selected)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var submission = await db.ContactSubmissions.FindAsync(selected.Id);
        if (submission is null) return;
        db.ContactSubmissions.Remove(submission);
        await db.SaveChangesAsync();
        IsError = false;
        StatusMessage = $"Message from {submission.Name} has been deleted successfully.";
        await LoadAsync();
    }
}
