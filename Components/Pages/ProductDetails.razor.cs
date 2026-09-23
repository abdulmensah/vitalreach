#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class ProductDetails
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Inject] private CommerceService Commerce { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string? CartMessage;
    private bool Adding;
    private async Task AddToCart()
    {
        if (Adding || SelectedVariant?.CanOrder != true) return;
        Adding = true;
        try { await Commerce.AddAsync(SelectedVariant.Id); Navigation.NavigateTo("/cart"); }
        catch (InvalidOperationException ex) { CartMessage = ex.Message; }
        catch (DbUpdateException) { CartMessage = "Your bag could not be updated. Please retry."; }
        finally { Adding = false; }
    }
    [Parameter] public string Slug { get; set; } = "";

    private ProductEntity? Product;
    private ProductEntity? Previous;
    private ProductEntity? Next;
    private HeadquartersSettings? Headquarters;
    private List<GalleryImage> GalleryImages = [];
    private string? SelectedImageUrl;
    private string SelectedImageAlt = "";
    private int SelectedVariantId;
    private ProductVariant? SelectedVariant => Product?.Variants.FirstOrDefault(v => v.Id == SelectedVariantId);
    private void VariantChanged()
    {
        SelectImage(SelectedVariant?.ImageUrl is { Length: > 0 } url
            ? new GalleryImage(url, $"{Product?.Name} — {SelectedVariant.DisplayName}") : GalleryImages.FirstOrDefault());
    }
    private string ContactInquiryUrl => Product is null
        ? "#"
        : $"/contact?message={Uri.EscapeDataString($"Hello VitalReach,\n\nI am interested in {Product.Name} — {SelectedVariant?.DisplayName} (SKU: {SelectedVariant?.Sku}, listed price: {SelectedVariant?.Price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}). Please confirm availability and let me know how I can purchase it.\n\nThank you.")}";

    protected override async Task OnParametersSetAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var products = await db.Products.AsNoTracking().Include(x => x.Variants).Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        var index = products.FindIndex(x => string.Equals(x.Slug, Slug, StringComparison.OrdinalIgnoreCase));
        Product = index >= 0 ? products[index] : null;
        GalleryImages = [];
        if (Product is not null)
        {
            if (!string.IsNullOrWhiteSpace(Product.ImageUrl))
                GalleryImages.Add(new GalleryImage(Product.ImageUrl, Product.Name));
            var additionalImages = await db.ProductImages.AsNoTracking()
                .Where(image => image.ProductId == Product.Id)
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => new GalleryImage(image.ImageUrl, image.AltText))
                .ToListAsync();
            GalleryImages.AddRange(additionalImages.Where(image => GalleryImages.All(existing => existing.Url != image.Url)));
        }
        SelectedVariantId = Product?.Variants.FirstOrDefault(v => v.CanOrder)?.Id ?? Product?.Variants.FirstOrDefault()?.Id ?? 0;
        VariantChanged();
        Previous = index > 0 ? products[index - 1] : null;
        Next = index >= 0 && index < products.Count - 1 ? products[index + 1] : null;
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }

    private void SelectImage(GalleryImage? image)
    {
        SelectedImageUrl = image?.Url;
        SelectedImageAlt = image?.Alt ?? Product?.Name ?? "Product image";
    }

    private sealed record GalleryImage(string Url, string Alt);
}
