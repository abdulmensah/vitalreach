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
    private List<GalleryImage> GalleryImages = [];
    private string? SelectedImageUrl;
    private string SelectedImageAlt = "";
    private string ContactInquiryUrl => Product is null
        ? "#"
        : $"/contact?message={Uri.EscapeDataString($"Hello VitalReach,\n\nI am interested in {Product.Name}. Please confirm availability and let me know how I can purchase it.\n\nThank you.")}";

    protected override async Task OnParametersSetAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var products = await db.Products.AsNoTracking().Where(x => x.IsPublished)
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
        SelectImage(GalleryImages.FirstOrDefault());
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
