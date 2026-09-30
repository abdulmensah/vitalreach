#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components;

public partial class IntakeQuestions
{
    [Parameter, EditorRequired] public IntakeEntry Entry { get; set; } = null!;
    [Parameter, EditorRequired] public IEnumerable<IntakeQuestion> Questions { get; set; } = [];
    [Parameter] public EventCallback Changed { get; set; }
    private async Task ChangeAsync(string id, ChangeEventArgs args)
    {
        Entry.Answers[id] = args.Value?.ToString() ?? "Unknown";
        await Changed.InvokeAsync();
    }
}
