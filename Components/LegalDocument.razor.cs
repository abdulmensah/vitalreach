#nullable enable
using Microsoft.AspNetCore.Components;

namespace VitalReach.Web.Components;

public sealed record LegalSectionLink(string Id, string Title);
public partial class LegalDocument
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter, EditorRequired] public string Description { get; set; } = "";
    [Parameter, EditorRequired] public string Path { get; set; } = "";
    [Parameter, EditorRequired] public IReadOnlyList<LegalSectionLink> Sections { get; set; } = [];
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = null!;
}
