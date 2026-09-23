#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components.Pages;
public partial class Cart
{
    [Inject] private CommerceService Commerce { get; set; } = default!;
    private CartView? Bag;
    private string? Message;
    private bool Busy;
    private static string Money(decimal value) => value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    protected override async Task OnInitializedAsync()
    {
        try { Bag = await Commerce.GetCartAsync(); }
        catch (InvalidOperationException ex) { Message = ex.Message; }
    }
    private Task Change(CartRow row, int amount) => Set(row.VariantId, row.Quantity + amount);
    private Task Remove(CartRow row) => Set(row.VariantId, 0);
    private async Task Set(int id, int quantity)
    {
        if (Busy) return;
        Busy = true; Message = null;
        try { await Commerce.SetQuantityAsync(id, quantity); }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (DbUpdateException) { Message = "Your bag changed in another tab. Review it and try again."; }
        finally { Bag = await Commerce.GetCartAsync(); Busy = false; }
    }
}
