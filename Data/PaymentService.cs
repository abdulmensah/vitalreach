using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class PaymentService(IDbContextFactory<CatalogDbContext> factory, CartSession session, IHttpClientFactory clients, IConfiguration config)
{
    public async Task<string> StartAsync(string number)
    {
        await using var db = await factory.CreateDbContextAsync();
        var order = await db.CommerceOrders.SingleOrDefaultAsync(x => x.Number == number && x.CartId == session.Id);
        if (order is null || order.Status != OrderStatus.AwaitingPayment || order.PaymentAmount is null or <= 0 || string.IsNullOrEmpty(order.TaxEvidence))
            throw new InvalidOperationException("This order is not ready for payment.");
        var provider = order.PaymentCurrency == "GHS" ? "Paystack" : "Stripe";
        var secret = config[$"Payments:{provider}:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException($"{provider} payments are not configured yet. Please contact VitalReach.");
        var baseUrl = config["Commerce:PublicUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var origin) || origin.Scheme != "https")
            throw new InvalidOperationException("The secure checkout return URL has not been configured.");
        var attempt = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.OrderId == order.Id);
        if (attempt?.Paid == true) throw new InvalidOperationException("This order has already been paid. Refresh your orders.");
        if (attempt is null)
        {
            attempt = new PaymentAttempt { Id = $"vr-{Guid.NewGuid():N}", OrderId = order.Id, Provider = provider,
                Currency = order.PaymentCurrency, AmountMinor = checked((long)(order.PaymentAmount.Value * 100)) };
            db.PaymentAttempts.Add(attempt);
            order.Version = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync();
        }
        if (!string.IsNullOrEmpty(attempt.CheckoutUrl)) return attempt.CheckoutUrl;
        using var request = new HttpRequestMessage(HttpMethod.Post, provider == "Stripe"
            ? "https://api.stripe.com/v1/checkout/sessions" : "https://api.paystack.co/transaction/initialize");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        if (provider == "Stripe")
        {
            request.Headers.Add("Idempotency-Key", attempt.Id);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mode"] = "payment", ["customer_email"] = order.Email, ["client_reference_id"] = attempt.Id,
                ["metadata[attempt_id]"] = attempt.Id,
                ["line_items[0][price_data][currency]"] = attempt.Currency.ToLowerInvariant(),
                ["line_items[0][price_data][unit_amount]"] = attempt.AmountMinor.ToString(CultureInfo.InvariantCulture),
                ["line_items[0][price_data][product_data][name]"] = $"VitalReach order {order.Number} (confirmed items, shipping and tax)",
                ["line_items[0][quantity]"] = "1", ["success_url"] = $"{baseUrl}/orders?payment=returned",
                ["cancel_url"] = $"{baseUrl}/orders?payment=cancelled"
            });
        }
        else request.Content = JsonContent.Create(new { email = order.Email, amount = attempt.AmountMinor, currency = attempt.Currency,
            reference = attempt.Id, callback_url = $"{baseUrl}/orders?payment=returned" });
        using var response = await clients.CreateClient("payments").SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Payment setup was not confirmed. Retry or contact VitalReach; do not submit a duplicate order.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = provider == "Stripe" ? json.RootElement : json.RootElement.GetProperty("data");
        attempt.ProviderId = provider == "Stripe" ? data.GetProperty("id").GetString()! : attempt.Id;
        attempt.CheckoutUrl = data.GetProperty(provider == "Stripe" ? "url" : "authorization_url").GetString()!;
        if (!Uri.TryCreate(attempt.CheckoutUrl, UriKind.Absolute, out var checkout) || checkout.Scheme != "https"
            || (provider == "Stripe" ? checkout.Host != "checkout.stripe.com" : checkout.Host != "checkout.paystack.com"))
            throw new InvalidOperationException("The payment provider returned an unexpected checkout address.");
        await db.SaveChangesAsync();
        return attempt.CheckoutUrl;
    }

    public static bool ValidSignature(string provider, string body, string header, string secret, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(header)) return false;
        try
        {
            if (provider == "Stripe")
            {
                var parts = header.Split(',').Select(x => x.Trim().Split('=', 2)).Where(x => x.Length == 2).ToList();
                if (!long.TryParse(parts.FirstOrDefault(x => x[0] == "t")?[1], out var seconds) || Math.Abs(now.ToUnixTimeSeconds() - seconds) > 300) return false;
                var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{seconds}.{body}"));
                return parts.Where(x => x[0] == "v1").Any(x => CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(x[1])));
            }
            return CryptographicOperations.FixedTimeEquals(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)), Convert.FromHexString(header));
        }
        catch (FormatException) { return false; }
    }

    public async Task<bool> WebhookAsync(string provider, string body, string signature)
    {
        var secret = config[provider == "Stripe" ? "Payments:Stripe:WebhookSecret" : "Payments:Paystack:SecretKey"] ?? "";
        if (!ValidSignature(provider, body, signature, secret, DateTimeOffset.UtcNow)) return false;
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        string reference;
        string providerId;
        if (provider == "Stripe")
        {
            var eventType = root.GetProperty("type").GetString();
            if (eventType is not ("checkout.session.completed" or "checkout.session.async_payment_succeeded")) return true;
            var data = root.GetProperty("data").GetProperty("object");
            reference = data.GetProperty("client_reference_id").GetString() ?? "";
            providerId = data.GetProperty("id").GetString() ?? "";
        }
        else
        {
            if (root.GetProperty("event").GetString() != "charge.success") return true;
            reference = root.GetProperty("data").GetProperty("reference").GetString() ?? "";
            providerId = reference;
        }
        await VerifyAsync(provider, reference, providerId);
        return true;
    }

    private async Task VerifyAsync(string provider, string reference, string providerId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var attempt = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.Id == reference && x.Provider == provider);
        if (attempt is null || attempt.Paid) return;
        if (!string.IsNullOrEmpty(attempt.ProviderId) && attempt.ProviderId != providerId) throw new InvalidOperationException("Payment session mismatch.");
        using var request = new HttpRequestMessage(HttpMethod.Get, provider == "Stripe"
            ? $"https://api.stripe.com/v1/checkout/sessions/{Uri.EscapeDataString(providerId)}"
            : $"https://api.paystack.co/transaction/verify/{Uri.EscapeDataString(reference)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config[$"Payments:{provider}:SecretKey"]);
        using var response = await clients.CreateClient("payments").SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = provider == "Stripe" ? json.RootElement : json.RootElement.GetProperty("data");
        if (data.GetProperty(provider == "Stripe" ? "payment_status" : "status").GetString() != (provider == "Stripe" ? "paid" : "success")) return;
        var verifiedReference = data.GetProperty(provider == "Stripe" ? "client_reference_id" : "reference").GetString();
        if (verifiedReference != reference || data.GetProperty(provider == "Stripe" ? "amount_total" : "amount").GetInt64() != attempt.AmountMinor
            || !string.Equals(data.GetProperty("currency").GetString(), attempt.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Payment amount, currency, or reference mismatch.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.CommerceOrders.SingleAsync(x => x.Id == attempt.OrderId);
        if (order.Status != OrderStatus.AwaitingPayment) throw new InvalidOperationException("Order is not awaiting payment; reconciliation required.");
        order.Status = OrderStatus.Paid; order.PaymentReference = $"{provider}:{providerId}";
        order.Version = Guid.NewGuid().ToString("N");
        attempt.Paid = true; attempt.ProviderId = providerId;
        db.CommerceOrderEvents.Add(new CommerceOrderEvent { OrderId = order.Id, Actor = provider, Description = "Payment independently verified with provider." });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
