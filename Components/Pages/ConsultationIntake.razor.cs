#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class ConsultationIntake : IDisposable
{
    [Inject] private IntakeService Intake { get; set; } = null!;
    private IntakeEntry Entry { get; set; } = new();
    private EditContext FormContext = null!;
    private bool Busy;
    private string? Reference;
    private List<string> Errors = [];
    private List<ScreeningFlag> ImmediateFlags = [];
    private List<ScreeningFlag> SubmittedFlags = [];
    protected override void OnInitialized()
    {
        FormContext = new(Entry);
        FormContext.OnFieldChanged += FieldChanged;
    }
    private void FieldChanged(object? sender, FieldChangedEventArgs args) => RefreshSafety();
    private void RefreshSafety() => ImmediateFlags = IntakeScreening.Analyze(Entry, DateTime.UtcNow).Where(f => f.Priority >= 2).ToList();
    public void Dispose() => FormContext.OnFieldChanged -= FieldChanged;
    private void SelectForm(string id, ChangeEventArgs args)
    {
        if (args.Value is true) { if (!Entry.Forms.Contains(id)) Entry.Forms.Add(id); }
        else
        {
            Entry.Forms.Remove(id);
            foreach (var key in Entry.Answers.Keys.Where(k => k.StartsWith(id + ".", StringComparison.Ordinal)).ToList()) Entry.Answers.Remove(key);
            if (id == "diabetes") { Entry.Glucose = [new(), new(), new()]; Entry.Hba1c = null; Entry.Hba1cDate = null; }
            if (id == "kidney") { Entry.Egfr = null; Entry.EgfrDate = null; Entry.Uacr = null; Entry.UacrDate = null; }
            if (!Entry.Forms.Any(f => f is "bp" or "liver")) Entry.Medications = IntakeCatalog.MedicationNames.Select(n => new MedicationEntry { Name = n }).ToList();
            if (!Entry.Forms.Any(f => f is "bp" or "cardio" or "kidney" or "diabetes")) Entry.BloodPressure = [new(), new(), new()];
        }
        RefreshSafety();
    }
    private async Task SubmitAsync(EditContext context)
    {
        if (Busy) return;
        Errors = IntakeValidation.Errors(Entry, DateTime.UtcNow);
        if (!context.Validate()) Errors.Add("Correct the invalid number or date fields before submitting.");
        if (Errors.Count > 0) return;
        Busy = true;
        try
        {
            var id = await Intake.SubmitAsync(Entry);
            SubmittedFlags = IntakeScreening.Analyze(Entry, DateTime.UtcNow).Where(f => f.Priority >= 2).ToList();
            Reference = id.ToString();
            Entry = new();
            ImmediateFlags.Clear();
        }
        catch (InvalidOperationException ex) { Errors = [ex.Message]; }
        catch (Exception) { Errors = ["We could not confirm submission. Please try again or ask center staff for help. Your form remains here."]; }
        finally { Busy = false; }
    }
}
