#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class MissionArticle
{
    [Parameter] public int Id { get; set; }
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    private MissionActivity? Activity;
    protected override async Task OnParametersSetAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Activity = await db.MissionActivities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Id && x.IsPublished);
    }
}
