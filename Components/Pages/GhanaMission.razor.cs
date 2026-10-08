#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class GhanaMission
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    private List<MissionActivity> Activities = [];
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Activities = await MissionActivitySchema.Showcase(db).ToListAsync();
    }
}
