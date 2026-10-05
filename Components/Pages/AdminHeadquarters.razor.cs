#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminHeadquarters
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminConfirmation Confirmation { get; set; } = default!;
    private List<Location> Locations = [];
    private Location? Draft;
    private string? Message;
    private bool IsError;
    private bool Saving;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Locations = await db.Locations.AsNoTracking().OrderBy(x => x.Rank).ThenBy(x => x.Id).ToListAsync();
    }
    private Task NewAsync() => Confirmation.RunAsync("Start a new location? Any unsaved edits will be discarded.", () => { Draft = new Location(); Message = null; });
    private Task EditAsync(Location location) => Confirmation.RunAsync("Edit this location? Any unsaved edits will be discarded.", async () =>
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Draft = await db.Locations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == location.Id);
        Message = Draft is null ? "This location no longer exists. Refresh the list." : null;
        IsError = Draft is null;
    });
    private Task SaveAsync() => Confirmation.RunAsync("Save this location? Only Published locations appear on the website, in rank order (lowest first).", SaveCoreAsync);
    private async Task SaveCoreAsync()
    {
        if (Draft is null) return;
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(Draft, new ValidationContext(Draft), errors, true))
        { IsError = true; Message = string.Join(" ", errors.Select(x => x.ErrorMessage)); return; }
        Saving = true;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            if (Draft.Id == 0) db.Locations.Add(Draft);
            else
            {
                db.Locations.Attach(Draft);
                db.Entry(Draft).State = EntityState.Modified;
                Draft.Version++;
            }
            await db.SaveChangesAsync();
            Draft = null;
            await LoadAsync();
            IsError = false;
            Message = "Location saved successfully. Published locations appear when visitors next load a page.";
        }
        catch (DbUpdateConcurrencyException)
        { Draft = null; IsError = true; Message = "Another administrator changed this location. Select it again to load the latest details."; await LoadAsync(); }
        catch (DbUpdateException)
        { IsError = true; Message = "Location could not be saved. Reload it before trying again."; Draft = null; }
        finally { Saving = false; }
    }
}
