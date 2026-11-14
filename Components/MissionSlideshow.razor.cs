#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace VitalReach.Web.Components;

public partial class MissionSlideshow : IAsyncDisposable
{
    private sealed record Slide(string File, string Alt, string Caption)
    {
        public string Image => $"/images/mission/{File}.jpg";
    }

    private static readonly Slide[] Slides =
    [
        new("health-screen-04", "Community members, including a wheelchair user, seated with healthcare workers at screening tables", "Making time for community health screenings."),
        new("madarasa-visit-donations", "Young people standing together indoors during a donation visit", "Coming together during a madarasa donation visit."),
        new("orphanage-visit-outdoors", "Children and adults gathered outdoors beside bags and boxes of donated supplies", "Everyday essentials shared during an orphanage visit."),
        new("orphanage-visit-presentation-2", "A group of children and young people gathered indoors during a visit", "Moments of connection during a community visit."),
        new("orphanage-visit-together-remastered", "A woman in a VitalReach shirt seated with children beside donated shoes and supplies", "Sharing time, care and company."),
        new("rashias-community-borehole-donations", "Two men standing beside a community water tank and a Rashias Foundation dedication plaque", "Water-access support through Rashias Foundation’s community work."),
        new("health-screen-01", "A community health outreach event with consultation tables beneath outdoor canopies", "Bringing healthcare outreach into the community.")
    ];

    [Inject] private IJSRuntime Javascript { get; set; } = default!;
    private ElementReference Hero;
    private IJSObjectReference? Module;
    private DotNetObjectReference<MissionSlideshow>? Reference;
    private bool Disposed;
    private bool IsPaused = true;
    private int ActiveIndex;
    private Slide Current => Slides[ActiveIndex];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        Module = await Javascript.InvokeAsync<IJSObjectReference>("import", "/js/mission-slideshow.js?v=1");
        if (Disposed) { await Module.DisposeAsync(); return; }
        Reference = DotNetObjectReference.Create(this);
        IsPaused = await Module.InvokeAsync<bool>("start", Hero, Reference);
        if (!Disposed) StateHasChanged();
    }

    private async Task Select(int index)
    {
        ActiveIndex = index;
        IsPaused = true;
        if (Module is not null) await Module.InvokeVoidAsync("setPaused", Hero, true);
    }
    private Task Previous() => Select((ActiveIndex + Slides.Length - 1) % Slides.Length);
    private Task Next() => Select((ActiveIndex + 1) % Slides.Length);
    private async Task ToggleRotation()
    {
        IsPaused = !IsPaused;
        if (Module is not null) await Module.InvokeVoidAsync("setPaused", Hero, IsPaused);
    }

    [JSInvokable]
    public Task AdvanceAutomatically() => InvokeAsync(() =>
    {
        if (Disposed || IsPaused) return;
        ActiveIndex = (ActiveIndex + 1) % Slides.Length;
        StateHasChanged();
    });

    [JSInvokable]
    public Task PauseFromBrowser() => InvokeAsync(() =>
    {
        if (Disposed) return;
        IsPaused = true;
        StateHasChanged();
    });

    public async ValueTask DisposeAsync()
    {
        Disposed = true;
        if (Module is not null)
        {
            try
            {
                await Module.InvokeVoidAsync("dispose", Hero);
                await Module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
        }
        Reference?.Dispose();
    }
}
