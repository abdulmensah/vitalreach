#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminMissionActivities
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminConfirmation Confirmation { get; set; } = default!;
    [Inject] private ProductImageStorage Images { get; set; } = default!;
    private List<MissionActivity> Activities = [];
    private MissionActivity? Draft;
    private IBrowserFile? SelectedImage;
    private bool Saving, IsError, RemoveImage;
    private string? Message;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Activities = await db.MissionActivities.AsNoTracking().OrderByDescending(x => x.ActivityDate).ThenByDescending(x => x.Id).ToListAsync();
    }
    private Task NewAsync()
    {
        if (!Saving && Draft is null) { Draft = new(); SelectedImage = null; RemoveImage = false; Message = null; IsError = false; }
        return Task.CompletedTask;
    }
    private async Task EditAsync(int id)
    {
        if (Saving || Draft is not null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        Draft = await db.MissionActivities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        SelectedImage = null; RemoveImage = false;
        IsError = Draft is null; Message = Draft is null ? "This activity no longer exists. Refresh the list." : null;
    }
    private void SelectImage(InputFileChangeEventArgs args) { SelectedImage = args.File; }
    private Task CloseEditorAsync() => Saving ? Task.CompletedTask : Confirmation.RunAsync("Close this editor and discard unsaved changes?", () => { Draft = null; Message = null; });
    private Task SaveAsync() => Confirmation.RunAsync("Save this activity and its publication settings? Published articles are publicly accessible; selected articles also appear on the mission timeline.", SaveCoreAsync);
    private async Task SaveCoreAsync()
    {
        if (Draft is null || Saving) return;
        Draft.Title = Draft.Title.Trim(); Draft.Location = Draft.Location.Trim();
        Draft.Summary = Draft.Summary.Trim(); Draft.Story = Draft.Story.Trim();
        Draft.VideoUrl = Draft.VideoUrl.Trim(); Draft.ImageAlt = Draft.ImageAlt.Trim();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(Draft, new ValidationContext(Draft), errors, true)
            || (SelectedImage is not null && Draft.ImageAlt.Length == 0))
        { IsError = true; Message = errors.Count > 0 ? string.Join(" ", errors.Select(x => x.ErrorMessage)) : "Add a photo description before saving."; return; }
        Saving = true;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            if (RemoveImage) Draft.ImageUrl = "";
            if (SelectedImage is not null)
            {
                Draft.ImageUrl = await Images.SaveAsync(SelectedImage);
                SelectedImage = null;
                RemoveImage = false;
            }
            if (Draft.Id == 0) db.MissionActivities.Add(Draft);
            else
            {
                db.MissionActivities.Attach(Draft);
                db.Entry(Draft).State = EntityState.Modified;
                Draft.Version++;
            }
            await db.SaveChangesAsync();
            Draft = null; await LoadAsync(); IsError = false;
            Message = "Activity saved. Publication changes appear when visitors reload the page.";
        }
        catch (DbUpdateConcurrencyException)
        { Draft = null; IsError = true; Message = "Another administrator changed this article. Open it again to load the latest version."; await LoadAsync(); }
        catch (DbUpdateException)
        { Draft = null; IsError = true; Message = "The activity could not be saved. Reload the article before trying again."; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        { IsError = true; Message = "The photo could not be saved. Choose a valid JPG, PNG or WebP image up to 5 MB and try again."; }
        finally { Saving = false; }
    }
}
