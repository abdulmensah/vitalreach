using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed record TaxQuote(decimal Tax, string Evidence);
public sealed class TaxQuoteService(IHttpClientFactory clients, IConfiguration config, IDbContextFactory<CatalogDbContext> factory)
{
    // Stripe Tax requires registrations, the seller's actual origin address, and product classifications in its dashboard.
    public async Task<TaxQuote> CalculateAsync(CommerceOrder order, decimal shipping)
    {
        if (order.Country != "US") throw new InvalidOperationException("Ghana requires a reviewed local tax quote; automated Ghana tax is not configured.");
        if (!config.GetValue<bool>("Commerce:UsTaxConfigured") || string.IsNullOrWhiteSpace(config["Payments:Stripe:SecretKey"]))
            throw new InvalidOperationException("Configure the Maryland seller address, registrations, and Stripe Tax before calculating US tax.");
        if (shipping < 0 || decimal.Round(shipping, 2) != shipping) throw new InvalidOperationException("Enter the confirmed shipping amount first.");
        await using var db = await factory.CreateDbContextAsync();
        var data = new Dictionary<string, string>
        {
            ["currency"] = "usd", ["customer_details[address_source]"] = "shipping",
            ["customer_details[address][line1]"] = order.Address, ["customer_details[address][city]"] = order.City,
            ["customer_details[address][state]"] = order.Region, ["customer_details[address][postal_code]"] = order.PostalCode,
            ["customer_details[address][country]"] = "US", ["shipping_cost[amount]"] = Minor(shipping),
            ["shipping_cost[tax_behavior]"] = "exclusive"
        };
        for (var index = 0; index < order.Items.Count; index++)
        {
            var line = order.Items[index];
            var code = await (from v in db.ProductVariants join p in db.Products on v.ProductId equals p.Id
                where v.Id == line.VariantId select p.TaxCode).SingleOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException($"Assign the correct Stripe tax code to {line.ProductName} first.");
            data[$"line_items[{index}][amount]"] = Minor(line.UnitPrice * line.Quantity);
            data[$"line_items[{index}][quantity]"] = line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
            data[$"line_items[{index}][reference]"] = line.Sku;
            data[$"line_items[{index}][tax_code]"] = code;
            data[$"line_items[{index}][tax_behavior]"] = "exclusive";
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/tax/calculations") { Content = new FormUrlEncodedContent(data) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["Payments:Stripe:SecretKey"]);
        using var response = await clients.CreateClient("payments").SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Tax calculation failed. Check the address, product tax codes, and Stripe Tax configuration.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        return new(root.GetProperty("tax_amount_exclusive").GetInt64() / 100m,
            $"Stripe Tax {root.GetProperty("id").GetString()}; shipping USD {shipping:0.00}; calculated {DateTime.UtcNow:O}. Review before confirming the quote.");
    }
    private static string Minor(decimal value) => checked((long)decimal.Round(value * 100, 0, MidpointRounding.AwayFromZero)).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
