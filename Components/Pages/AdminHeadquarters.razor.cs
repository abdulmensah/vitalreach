#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminHeadquarters
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;

    private HeadquartersSettings? Settings;
    private string? Message;
    private bool IsError;
    private bool Saving;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Settings = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }

    private async Task SaveAsync()
    {
        if (Settings is null) return;
        Saving = true;
        try
        {
            Settings.UpdatedUtc = DateTimeOffset.UtcNow;
            await using var db = await DbFactory.CreateDbContextAsync();
            db.Headquarters.Update(Settings);
            await db.SaveChangesAsync();
            IsError = false;
            Message = "Headquarters details have been saved successfully.";
        }
        catch (DbUpdateException)
        {
            IsError = true;
            Message = "Headquarters details could not be saved. Please try again.";
        }
        finally
        {
            Saving = false;
        }
    }
}
