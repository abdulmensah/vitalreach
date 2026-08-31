#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace VitalReach.Web.Components;

public partial class AdminPortalLayout
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;
    [Parameter, EditorRequired] public string ActiveSection { get; set; } = "";

    private string AdminEmail { get; set; } = "Administrator";
    private bool NavigationOpen { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        AdminEmail = state.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Administrator";
    }

    private string ActiveClass(string section) => ActiveSection == section ? "active" : "";
    private void ToggleNavigation() => NavigationOpen = !NavigationOpen;
}
