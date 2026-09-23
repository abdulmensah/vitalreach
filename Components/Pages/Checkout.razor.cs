#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components.Pages;
public partial class Checkout
{
    [Inject] private CommerceService Commerce { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private CheckoutInput Input = new();
    private CartView? Bag;
    private string? Message;
    private bool Busy;
    private static string Money(decimal value) => value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    protected override async Task OnInitializedAsync()
    {
        try { Bag = await Commerce.GetCartAsync(); }
        catch (InvalidOperationException ex) { Message = ex.Message; }
    }
    private async Task Submit()
    {
        if (Busy || Bag is null) return;
        Busy = true;
        try
        {
            var number = await Commerce.CheckoutAsync(Input, Bag.Revision, Bag.Fingerprint);
            Navigation.NavigateTo($"/orders?submitted={Uri.EscapeDataString(number)}");
        }
        catch (InvalidOperationException ex) { Message = ex.Message; Bag = await Commerce.GetCartAsync(); }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex) { Message = ex.Message; }
        catch (DbUpdateException) { Message = "Your order could not be saved. Check your orders before trying again."; }
        finally { Busy = false; }
    }
}
