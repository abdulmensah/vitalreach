#nullable enable

namespace VitalReach.Web.Components;

public partial class StorefrontHeader
{
    [Microsoft.AspNetCore.Components.Parameter] public bool MissionTheme { get; set; }

    private bool MenuOpen;

    private void ToggleMenu() => MenuOpen = !MenuOpen;
    private void CloseMenu() => MenuOpen = false;
}
