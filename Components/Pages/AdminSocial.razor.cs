#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminSocial
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminConfirmation Confirmation { get; set; } = default!;
    private List<SocialLink> Links = [];
    private SocialLink? Draft;
    private string? Message;
    private bool IsError;
    private bool Saving;
    private void PlatformChanged()
    {
        if (Draft is not null) Draft.Label = Draft.Platform == SocialPlatform.Other ? "" : Draft.Platform.ToString();
    }
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Links = await db.SocialLinks.AsNoTracking().OrderBy(x => x.Rank).ThenBy(x => x.Id).ToListAsync();
    }
    private Task NewAsync() => Confirmation.RunAsync("Start a new social profile? Any unsaved edits will be discarded.", () => { Draft = new SocialLink(); Message = null; });
    private Task EditAsync(SocialLink link) => Confirmation.RunAsync("Edit this social profile? Any unsaved edits will be discarded.", async () =>
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Draft = await db.SocialLinks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == link.Id);
        Message = Draft is null ? "This social profile no longer exists. Refresh the list." : null;
        IsError = Draft is null;
    });
    private Task SaveAsync() => Confirmation.RunAsync("Save this social profile? Only published profiles appear on the website, in rank order (lowest first).", SaveCoreAsync);
    private async Task SaveCoreAsync()
    {
        if (Draft is null) return;
        Draft.Url = Draft.Url?.Trim() ?? "";
        Draft.Label = Draft.Label?.Trim() ?? "";
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(Draft, new ValidationContext(Draft), errors, true))
        { IsError = true; Message = string.Join(" ", errors.Select(x => x.ErrorMessage)); return; }
        Saving = true;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            if (Draft.Id == 0) db.SocialLinks.Add(Draft);
            else
            {
                db.SocialLinks.Attach(Draft);
                db.Entry(Draft).State = EntityState.Modified;
                Draft.Version++;
            }
            await db.SaveChangesAsync();
            Draft = null;
            await LoadAsync();
            IsError = false;
            Message = "Social profile saved successfully. Published social profiles appear when visitors next load a page.";
        }
        catch (DbUpdateConcurrencyException)
        { Draft = null; IsError = true; Message = "Another administrator changed this social profile. Select it again to load the latest details."; await LoadAsync(); }
        catch (DbUpdateException)
        { IsError = true; Message = "Social profile could not be saved. Reload it before trying again."; Draft = null; }
        finally { Saving = false; }
    }
}
