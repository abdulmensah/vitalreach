#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace VitalReach.Web.Components;

public partial class AdminEditDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime Javascript { get; set; } = default!;
    [Parameter] public string Title { get; set; } = "Edit";
    [Parameter] public bool Busy { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private readonly string TitleId = $"admin-editor-{Guid.NewGuid():N}";
    private ElementReference Dialog;
    private IJSObjectReference? Module;
    private DotNetObjectReference<AdminEditDialog>? Reference;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        Module = await Javascript.InvokeAsync<IJSObjectReference>("import", "/js/admin-edit-dialog.js?v=1");
        Reference = DotNetObjectReference.Create(this);
        await Module.InvokeVoidAsync("open", Dialog, Reference);
    }
    [JSInvokable] public Task RequestCloseAsync() => Busy ? Task.CompletedTask : OnClose.InvokeAsync();
    public async ValueTask DisposeAsync()
    {
        if (Module is not null)
        {
            try { await Module.InvokeVoidAsync("dispose", Dialog); await Module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
        }
        Reference?.Dispose();
    }
}
