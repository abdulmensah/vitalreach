#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class ProductDetails
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Parameter] public string Slug { get; set; } = "";

    private ProductEntity? Product;
    private ProductEntity? Previous;
    private ProductEntity? Next;
    private HeadquartersSettings? Headquarters;
    private string PurchaseEmail => Product is null
        ? "#"
        : $"mailto:{Headquarters?.Email ?? "hello@vitalreachwellness.com"}?subject={Uri.EscapeDataString($"Product inquiry: {Product.Name}")}";

    protected override async Task OnParametersSetAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var products = await db.Products.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        var index = products.FindIndex(x => string.Equals(x.Slug, Slug, StringComparison.OrdinalIgnoreCase));
        Product = index >= 0 ? products[index] : null;
        Previous = index > 0 ? products[index - 1] : null;
        Next = index >= 0 && index < products.Count - 1 ? products[index + 1] : null;
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }
}
