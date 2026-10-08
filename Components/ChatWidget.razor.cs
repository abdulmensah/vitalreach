#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components;
public partial class ChatWidget : IDisposable
{
    [Inject] private ChatService Chat { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    private RecaptchaChallenge? Challenge;
    private bool Open, Consent, Busy;
    private string Text = "";
    private string? Error;
    private bool Visible => Configuration.GetValue<bool>("Chatbot:Enabled") && (new Uri(Navigation.Uri).AbsolutePath.TrimEnd('/') is "" or "/shop" or "/health-guide" or "/wellness-center" or "/contact");
    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => _ = InvokeAsync(() => { Open = false; StateHasChanged(); });
    private void Toggle() => Open = !Open;
    private void End() { Chat.End(); Open = false; Consent = false; Text = ""; Error = null; }
    private async Task StartAsync()
    {
        if (Busy || !Consent) return;
        Busy = true;
        try
        {
            Error = await Chat.StartAsync(Challenge is null ? null : await Challenge.GetTokenAsync("support_start"))
                ? null : "We could not verify your request. Please try again or use our contact page.";
        }
        finally { Busy = false; }
    }
    private async Task SendAsync()
    {
        if (Busy) return;
        Busy = true;
        try { Error = await Chat.SendAsync(Text, Challenge is null ? null : await Challenge.GetTokenAsync("support_chat")); if (Error is null) Text = ""; }
        finally { Busy = false; }
    }
    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
