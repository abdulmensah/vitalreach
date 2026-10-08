#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components;

public partial class RecaptchaChallenge : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private RecaptchaService Captcha { get; set; } = default!;
    [Inject] private IHostEnvironment Environment { get; set; } = default!;
    private bool IsDevelopment => Environment.IsDevelopment();
    private IJSObjectReference? Module;
    private bool Failed;
    private string? DevelopmentError;

    public async Task<string?> GetTokenAsync(string action)
    {
        if (Captcha.DevelopmentBypass || !Captcha.IsConfigured) return null;
        try
        {
            Module ??= await JS.InvokeAsync<IJSObjectReference>("import", "/js/recaptcha.js?v=3");
            var token = await Module.InvokeAsync<string>("execute", Captcha.SiteKey, action, Captcha.UseRecaptchaNet);
            Failed = false;
            DevelopmentError = null;
            return token;
        }
        catch (Exception ex) when (ex is JSException or OperationCanceledException)
        {
            Failed = true;
            DevelopmentError = ex is OperationCanceledException ? "Browser verification timed out."
                : ex.Message.Contains("Invalid site key", StringComparison.OrdinalIgnoreCase) ? "Google rejected the site key or key type. Verify that it is a v3 score-based key."
                : ex.Message.Contains("Failed to fetch dynamically imported module", StringComparison.OrdinalIgnoreCase) ? "The local verification script could not load. Refresh the page after restarting the app."
                : "The browser could not load or execute Google verification. Check browser console errors and content blockers.";
            return null;
        }
        finally { await InvokeAsync(StateHasChanged); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Module is null) return;
        try { await Module.DisposeAsync(); }
        catch (Exception ex) when (ex is JSException or OperationCanceledException) { }
    }
}
