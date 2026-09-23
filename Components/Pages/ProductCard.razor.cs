#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class ProductCard
{
    [Parameter, EditorRequired] public ProductEntity Product { get; set; } = default!;
    [Parameter] public EventCallback<VariantSelection> OnAdd { get; set; }
    private int SelectedVariantId;
    private ProductVariant? SelectedVariant => Product.Variants.FirstOrDefault(v => v.Id == SelectedVariantId);
    protected override void OnParametersSet()
    {
        if (!Product.Variants.Any(v => v.Id == SelectedVariantId))
            SelectedVariantId = Product.Variants.FirstOrDefault(v => v.CanOrder)?.Id ?? Product.Variants.FirstOrDefault()?.Id ?? 0;
    }
    private Task Add() => SelectedVariant is { CanOrder: true } variant
        ? OnAdd.InvokeAsync(new VariantSelection(Product, variant)) : Task.CompletedTask;
    public sealed record VariantSelection(ProductEntity Product, ProductVariant Variant);
}
