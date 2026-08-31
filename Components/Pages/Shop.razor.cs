#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class Shop
{
    private const int PageSize = 3;

    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "q")] public string? Search { get; set; }
    [SupplyParameterFromQuery(Name = "category")] public string? Category { get; set; }
    [SupplyParameterFromQuery(Name = "page")] public int PageNumber { get; set; } = 1;

    private List<ProductEntity> Products = [];
    private List<CategoryFacet> Facets = [];
    private HeadquartersSettings? Headquarters;
    private int TotalProducts;
    private int TotalAcrossCategories;
    private int TotalPages = 1;

    protected override async Task OnParametersSetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        await using var db = await DbFactory.CreateDbContextAsync();
        var published = db.Products.AsNoTracking().Where(x => x.IsPublished);
        var facetRows = await published.GroupBy(x => x.Category)
            .Select(x => new { Name = x.Key, Count = x.Count() })
            .OrderBy(x => x.Name)
            .ToListAsync();
        Facets = facetRows.Select(x => new CategoryFacet(x.Name, x.Count)).ToList();
        TotalAcrossCategories = Facets.Sum(x => x.Count);

        var query = published;
        if (!string.IsNullOrWhiteSpace(Category)) query = query.Where(x => x.Category == Category);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Category.Contains(term) || x.Benefit.Contains(term) || x.Description.Contains(term));
        }

        TotalProducts = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalProducts / (double)PageSize));
        PageNumber = Math.Min(PageNumber, TotalPages);
        Products = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }

    private string BuildUrl(string? category, int page)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(Search)) values.Add($"q={Uri.EscapeDataString(Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(category)) values.Add($"category={Uri.EscapeDataString(category)}");
        if (page > 1) values.Add($"page={page}");
        return values.Count == 0 ? "/shop" : $"/shop?{string.Join("&", values)}";
    }

    private sealed record CategoryFacet(string Name, int Count);
}
