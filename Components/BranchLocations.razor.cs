#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components;
public partial class BranchLocations
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Parameter] public int? ExcludeId { get; set; }
    private List<Location> Locations = [];
    protected override async Task OnParametersSetAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Locations = await LocationSchema.Published(db).Where(x => x.Id != ExcludeId).ToListAsync();
    }
}
