#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components;
public partial class SocialLinks
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    private List<SocialLink> Links = [];
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var links = await db.SocialLinks.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.Rank).ThenBy(x => x.Id).ToListAsync();
        Links = links.Where(x => SocialLink.IsValidUrl(x.Platform, x.Url)).ToList();
    }
}
