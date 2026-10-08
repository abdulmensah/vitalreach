using Microsoft.JSInterop;

namespace VitalReach.Web.Data;

/// <summary>One confirmation and mutation at a time per admin circuit. Cancel/disconnect never executes the action.</summary>
public sealed class AdminConfirmation(IJSRuntime javascript)
{
    private bool pending;

    public async Task RunAsync(string message, Func<Task> action)
    {
        if (pending) return;
        pending = true;
        try
        {
            bool confirmed;
            try { confirmed = await javascript.InvokeAsync<bool>("adminConfirm.ask", message); }
            catch (JSDisconnectedException) { return; }
            catch (TaskCanceledException) { return; }
            if (confirmed) await action();
        }
        finally { pending = false; }
    }

    public Task RunAsync(string message, Action action) => RunAsync(message, () => { action(); return Task.CompletedTask; });
}
