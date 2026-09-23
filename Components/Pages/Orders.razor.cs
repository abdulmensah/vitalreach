#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components.Pages;
public partial class Orders
{
    [Inject] private CommerceService Commerce { get; set; } = default!;
    [Inject] private PaymentService Payments { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "submitted")] public string? Submitted { get; set; }
    [SupplyParameterFromQuery(Name = "payment")] public string? Payment { get; set; }
    private List<CommerceOrder> Items = [];
    private string? Message;
    private bool Busy;
    private static string Money(decimal value) => $"USD {value:N2}";
    private static string Amount(decimal? value) => value.HasValue ? Money(value.Value) : "To be confirmed";
    public static string StatusLabel(OrderStatus status) => status switch { OrderStatus.QuoteRequested => "Quote requested", OrderStatus.AwaitingPayment => "Awaiting payment", _ => status.ToString() };
    protected override async Task OnParametersSetAsync()
    {
        await Load();
        if (Submitted is not null && Items.Any(x => x.Number == Submitted)) Message = $"Order request {Submitted} was saved successfully. Check back for your confirmed quote.";
        if (Payment is not null) Message = "Payment status is confirmed by the provider. Refresh to see the latest status; returning here alone does not confirm payment.";
    }
    private async Task Load()
    {
        try { Items = await Commerce.MyOrdersAsync(); }
        catch (InvalidOperationException ex) { Message = ex.Message; }
    }
    private async Task Pay(CommerceOrder order)
    {
        if (Busy) return;
        Busy = true;
        try { Navigation.NavigateTo(await Payments.StartAsync(order.Number), forceLoad: true); }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (HttpRequestException) { Message = "The payment provider could not be reached. Please retry shortly."; }
        catch (TaskCanceledException) { Message = "The payment provider did not respond in time. Please retry shortly."; }
        catch (DbUpdateException) { Message = "Payment setup changed in another request. Refresh and try again."; }
        finally { Busy = false; }
    }
}
