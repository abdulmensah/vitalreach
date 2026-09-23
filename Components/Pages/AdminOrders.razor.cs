#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components.Pages;
public partial class AdminOrders
{
    [Inject] private CommerceService Commerce { get; set; } = default!;
    [Inject] private TaxQuoteService TaxQuotes { get; set; } = default!;
    private List<CommerceOrder> Items = [];
    private CommerceOrder? Selected;
    private string Search = "", Status = "", Reference = "", Instructions = "", TaxEvidence = "";
    private decimal Shipping, Tax, ExchangeRate = 1;
    private string? Message;
    private bool Busy;
    private static string Money(decimal amount) => $"USD {amount:N2}";
    private IEnumerable<CommerceOrder> Filtered => Items.Where(o => (Status == "" || o.Status.ToString() == Status)
        && (string.IsNullOrWhiteSpace(Search) || $"{o.Number} {o.CustomerName} {o.Email} {string.Join(' ', o.Items.Select(i => i.Sku))}".Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase)));
    protected override Task OnInitializedAsync() => Load();
    private async Task Load()
    {
        Items = await Commerce.OrdersAsync();
        if (Selected is not null) { var updated = Items.FirstOrDefault(x => x.Id == Selected.Id); if (updated is not null) Select(updated); }
    }
    private void Select(CommerceOrder order)
    {
        Selected = order; Shipping = order.Shipping ?? 0; Tax = order.Tax ?? 0; Reference = "";
        ExchangeRate = order.Country == "GH" && order.Status == OrderStatus.QuoteRequested ? 0 : order.ExchangeRate;
        Instructions = order.PaymentInstructions; TaxEvidence = order.TaxEvidence;
    }
    private async Task CalculateTax()
    {
        if (Busy || Selected is null) return;
        Busy = true;
        try { var quote = await TaxQuotes.CalculateAsync(Selected, Shipping); Tax = quote.Tax; TaxEvidence = quote.Evidence; Message = "US tax calculated. Review the quote before confirming."; }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (HttpRequestException) { Message = "Tax service unavailable. No quote was issued."; }
        catch (TaskCanceledException) { Message = "Tax service timed out. Please try again."; }
        finally { Busy = false; }
    }
    private async Task Act(string action)
    {
        if (Busy || Selected is null) return;
        Busy = true;
        try
        {
            await Commerce.UpdateOrderAsync(Selected.Id, Selected.Version, action, Shipping, Tax, Reference, Instructions, TaxEvidence, ExchangeRate);
            await Load(); Message = "Order updated successfully.";
        }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (DbUpdateException) { Message = "The order changed or could not be saved. Refresh before trying again."; }
        finally { Busy = false; }
    }
}
