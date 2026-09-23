#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components.Pages;
public partial class Home
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Inject] private CommerceService Commerce { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private bool MenuOpen;
    private List<ProductEntity> Products = [];
    private HeadquartersSettings? Headquarters;
    private string? CartMessage;
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Products = await db.Products.AsNoTracking().Include(p => p.Variants).Where(p => p.IsPublished)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name).Take(4).ToListAsync();
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }
    private async Task AddToCart(ProductCard.VariantSelection selection)
    {
        try { await Commerce.AddAsync(selection.Variant.Id); Navigation.NavigateTo("/cart"); }
        catch (InvalidOperationException ex) { CartMessage = ex.Message; }
        catch (DbUpdateException) { CartMessage = "The bag changed in another session. Please try again."; }
    }
    private void ToggleMenu() => MenuOpen = !MenuOpen;
    private void CloseMenu() => MenuOpen = false;
}
