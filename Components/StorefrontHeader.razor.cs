#nullable enable

namespace VitalReach.Web.Components;

public partial class StorefrontHeader
{
    private bool MenuOpen;

    private void ToggleMenu() => MenuOpen = !MenuOpen;
    private void CloseMenu() => MenuOpen = false;
}
