#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components;
public partial class StorefrontFooter
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    private List<Location> Locations = [];
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Locations = await LocationSchema.Published(db).ToListAsync();
    }
}
