#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminConsultations
{
    [Inject] private IntakeService Intake { get; set; } = null!;
    private List<ConsultationSubmission> Rows = [];
    private IntakeDetail? Selected;
    private string Status = "Awaiting review", Message = "", ReviewStatus = "In review", ReviewNotes = "";
    private int Page;
    private bool Busy;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task RunAsync(Func<Task> action)
    {
        if (Busy) return;
        Busy = true; Message = "";
        try { await action(); }
        catch (UnauthorizedAccessException) { Close(); Rows = []; Message = "Your clinical reviewer access is no longer active."; }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (Exception) { Close(); Message = "The record could not be loaded or saved. Contact the system administrator; do not copy health details into support messages."; }
        finally { Busy = false; }
    }
    private Task LoadAsync() => RunAsync(async () => { Close(); Rows = await Intake.ListAsync(Status, Page); });
    private Task ResetPageAsync() { Page = 0; return LoadAsync(); }
    private Task PreviousAsync() { Page = Math.Max(0, Page - 1); return LoadAsync(); }
    private Task NextAsync() { Page++; return LoadAsync(); }
    private Task OpenAsync(Guid id) => RunAsync(async () =>
    {
        Close(); Selected = await Intake.ReadAsync(id);
        if (Selected is null) { Message = "The submission could not be found."; return; }
        ReviewStatus = Selected.Record.Status == "Reviewed" ? "Reviewed" : "In review";
        ReviewNotes = Selected.Review?.Notes ?? "";
    });
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (Selected is null) return;
        await Intake.ReviewAsync(Selected.Record.Id, Selected.Record.Version, ReviewStatus, ReviewNotes);
        Selected = await Intake.ReadAsync(Selected.Record.Id);
        Rows = await Intake.ListAsync(Status, Page);
        Message = "Clinical review saved.";
    });
    private void Close() { Selected = null; ReviewNotes = ""; }
}
