using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VitalReach.Web.Data;

var file = Path.Combine(Path.GetTempPath(), $"vitalreach-commerce-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite($"Data Source={file};Pooling=False").Options;
var factory = new TestFactory(options);
var auth = new TestAuth();
var authorization = new TestAuthorization();
var protection = new EphemeralDataProtectionProvider();
CartSession Session()
{
    var context = new DefaultHttpContext(); context.Items[CartSession.CookieName] = Guid.NewGuid().ToString("N");
    return new(new HttpContextAccessor { HttpContext = context }, protection);
}
var session = Session();
var service = new CommerceService(factory, session, auth, authorization);
var other = new CommerceService(factory, Session(), auth, authorization);
var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception($"FAIL: {description}");
    Console.WriteLine($"PASS: {description}"); checks++;
}
async Task Reject(Func<Task> action, string description)
{
    try
    {
        await action();
    }
    catch (InvalidOperationException) { Check(true, description); return; }
    throw new Exception($"FAIL: {description} did not reject");
}
CheckoutInput Input(string country = "US") => new()
{
    Name = "Test Buyer",
    Email = "test@example.com",
    Phone = "5551234567",
    Address = "1 Test Street",
    City = "Baltimore",
    Region = "MD",
    PostalCode = "21201",
    Country = country
};
async Task<CommerceOrder> Order(string number)
{
    await using var db = new CatalogDbContext(options);
    return await db.CommerceOrders.AsNoTracking().Include(x => x.Items).SingleAsync(x => x.Number == number);
}
async Task<int?> Stock(int id)
{
    await using var db = new CatalogDbContext(options); return (await db.ProductVariants.FindAsync(id))!.StockQuantity;
}
try
{
    await using (var db = new CatalogDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        db.Products.Add(new ProductEntity { Slug = "test", Name = "Test supplement", Category = "Supplements", Benefit = "Test", Detail = "60 capsules", Price = 20, TaxCode = "txcd_99999999" });
        await db.SaveChangesAsync();
        await VariantSchema.EnsureAsync(db);
        await CommerceSchema.EnsureAsync(db);
        Check(await db.ProductVariants.CountAsync() == 1, "fresh EF schema accepts default variant seeding");
        // Exercise the exact legacy upgrade path: existing products, no commerce or variants tables.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE PaymentAttempts; DROP TABLE CommerceOrderEvents; DROP TABLE CommerceOrderItems; DROP TABLE CommerceOrders; DROP TABLE ShoppingCartItems; DROP TABLE ShoppingCarts; DROP TABLE ProductVariants;");
        await VariantSchema.EnsureAsync(db); await VariantSchema.EnsureAsync(db);
        await CommerceSchema.EnsureAsync(db); await CommerceSchema.EnsureAsync(db);
        var variant = await db.ProductVariants.SingleAsync();
        Check(variant.Name == "60 capsules" && variant.Price == 20 && variant.StockQuantity == null, "legacy upgrade preserves price/detail and does not invent stock");
        variant.StockQuantity = 5;
        db.ProductVariants.Add(new ProductVariant { ProductId = variant.ProductId, Name = "120 capsules", Sku = "TEST-120", Price = 35, StockQuantity = 2 });
        await db.SaveChangesAsync();
    }
    Check(VariantValidation.Validate([new() { Sku = "same" }, new() { Sku = "SAME" }]) != null, "duplicate SKU validation is case-insensitive");
    Check(VariantValidation.Validate([new() { Sku = "X", NetContent = 250 }]) != null, "net content requires a unit");
    Check(VariantValidation.Validate([new() { Sku = "X", Price = 1.111m }]) != null, "fractional-cent prices rejected");
    await service.AddAsync(1); await service.AddAsync(1); await service.AddAsync(2);
    var bag = await service.GetCartAsync();
    Check(bag.Items.Count == 2 && bag.Subtotal == 75 && bag.Items.Single(x => x.VariantId == 1).Quantity == 2, "persistent cart merges same variant and separates other variants");
    Check((await other.GetCartAsync()).Items.Count == 0, "guest carts isolated");
    var reopened = new CommerceService(factory, session, auth, authorization);
    Check((await reopened.GetCartAsync()).Subtotal == 75, "new service instance restores persisted cart");
    await Reject(() => service.SetQuantityAsync(1, 6), "stock limit enforced");
    await using (var db = new CatalogDbContext(options))
    {
        (await db.ProductVariants.FindAsync(1))!.Price = 21; await db.SaveChangesAsync();
    }
    await Reject(() => service.CheckoutAsync(Input(), bag.Revision, bag.Fingerprint), "checkout rejects unreviewed price changes");
    bag = await service.GetCartAsync();
    var number = await service.CheckoutAsync(Input(), bag.Revision, bag.Fingerprint);
    Check(await service.CheckoutAsync(Input(), bag.Revision, bag.Fingerprint) == number, "duplicate checkout returns original order");
    Check((await service.GetCartAsync()).Items.Count == 0, "cart cleared atomically with order creation");
    Check((await other.MyOrdersAsync()).Count == 0, "guest order history isolated");
    var order = await Order(number);
    Check(order.Subtotal == 77 && order.Total == null && order.Items[0].Sku.Length > 0, "order stores purchase snapshots and unknown shipping/tax is not zero");
    await using (var db = new CatalogDbContext(options)) { (await db.ProductVariants.FindAsync(2))!.StockQuantity = 0; await db.SaveChangesAsync(); }
    await Reject(() => service.UpdateOrderAsync(order.Id, order.Version, "quote", 5, 4, "", "Test quote", "Tax reviewed", 1), "unavailable second line rejects entire quote");
    Check(await Stock(1) == 5, "failed quote rolls back first line stock reservation");
    await using (var db = new CatalogDbContext(options)) { (await db.ProductVariants.FindAsync(2))!.StockQuantity = 2; await db.SaveChangesAsync(); }
    await service.UpdateOrderAsync(order.Id, order.Version, "quote", 5, 4, "", "Test quote", "Tax reviewed", 1);
    Check(await Stock(1) == 3 && await Stock(2) == 1, "confirmed quote reserves inventory");
    await Reject(() => service.UpdateOrderAsync(order.Id, order.Version, "paid", 0, 0, "TEST", "", "", 1), "stale admin update rejected");
    order = await Order(number);
    await service.UpdateOrderAsync(order.Id, order.Version, "cancel", 0, 0, "", "", "", 1);
    Check(await Stock(1) == 5 && await Stock(2) == 2, "cancellation restores reserved inventory");
    await service.AddAsync(1); bag = await service.GetCartAsync();
    var ghNumber = await service.CheckoutAsync(Input("GH"), bag.Revision, bag.Fingerprint);
    var gh = await Order(ghNumber);
    await service.UpdateOrderAsync(gh.Id, gh.Version, "quote", 3, 2, "", "Confirmed quote", "Ghana tax review reference", 12);
    gh = await Order(ghNumber);
    Check(gh.PaymentCurrency == "GHS" && gh.PaymentAmount == 312, "Ghana quote stores explicit currency conversion");
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Commerce:PublicUrl"] = "https://example.com",
        ["Payments:Stripe:SecretKey"] = "stripe-test",
        ["Payments:Stripe:WebhookSecret"] = "whsec_test",
        ["Payments:Paystack:SecretKey"] = "paystack-test"
    }).Build();
    var gateway = new FakeGateway();
    var taxes = new TaxQuoteService(gateway, config, factory);
    await Reject(() => taxes.CalculateAsync(gh, 3), "Ghana tax requires local review");
    await Reject(() => taxes.CalculateAsync(order, 5), "US tax blocked until registrations configured");
    config["Commerce:UsTaxConfigured"] = "true";
    var taxQuote = await taxes.CalculateAsync(order, 5);
    Check(taxQuote.Tax == 4.62m && taxQuote.Evidence.Contains("taxcalc_test"), "US tax response stores calculated amount and evidence");
    var payments = new PaymentService(factory, session, gateway, config);
    var strangerPayments = new PaymentService(factory, Session(), gateway, config);
    await Reject(() => strangerPayments.StartAsync(ghNumber), "cannot pay another guest's order");
    Check((await payments.StartAsync(ghNumber)).StartsWith("https://checkout.paystack.com/"), "Paystack hosted checkout initialized with GHS quote");
    string attemptId;
    await using (var db = new CatalogDbContext(options)) { attemptId = (await db.PaymentAttempts.SingleAsync()).Id; }
    var body = JsonSerializer.Serialize(new { @event = "charge.success", data = new { reference = attemptId } });
    var signature = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes("paystack-test"), Encoding.UTF8.GetBytes(body)));
    Check(!await payments.WebhookAsync("Paystack", body, "bad"), "forged payment webhook rejected");
    gateway.AmountOverride = 1;
    await Reject(() => payments.WebhookAsync("Paystack", body, signature), "provider amount mismatch rejected");
    Check((await Order(ghNumber)).Status == OrderStatus.AwaitingPayment, "failed verification cannot mark order paid");
    gateway.AmountOverride = null;
    Check(await payments.WebhookAsync("Paystack", body, signature), "signed Paystack webhook verifies payment server-to-server");
    Check(await payments.WebhookAsync("Paystack", body, signature), "duplicate payment webhook accepted idempotently");
    Check((await Order(ghNumber)).Status == OrderStatus.Paid, "verified payment marks order paid");
    await service.AddAsync(2); bag = await service.GetCartAsync();
    var usNumber = await service.CheckoutAsync(Input(), bag.Revision, bag.Fingerprint);
    var us = await Order(usNumber);
    await service.UpdateOrderAsync(us.Id, us.Version, "quote", 5, 2, "", "Confirmed quote", "Tax review", 1);
    Check((await payments.StartAsync(usNumber)).StartsWith("https://checkout.stripe.com/"), "Stripe hosted checkout initialized with USD quote");
    await using (var db = new CatalogDbContext(options)) { attemptId = (await db.PaymentAttempts.SingleAsync(x => x.OrderId == us.Id)).Id; }
    var stripeBody = JsonSerializer.Serialize(new { type = "checkout.session.completed", data = new { @object = new { id = "cs_test", client_reference_id = attemptId } } });
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var hash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_test"), Encoding.UTF8.GetBytes($"{timestamp}.{stripeBody}")));
    Check(!PaymentService.ValidSignature("Stripe", stripeBody, $"t={timestamp},v1={hash}", "whsec_test", DateTimeOffset.UtcNow.AddMinutes(10)), "stale Stripe signatures rejected");
    Check(await payments.WebhookAsync("Stripe", stripeBody, $"t={timestamp},v1={hash}"), "Stripe webhook verified");
    us = await Order(usNumber);
    Check(us.Status == OrderStatus.Paid && us.PaymentAmount == 42, "Stripe payment amount uses confirmed quote");
    await service.UpdateOrderAsync(us.Id, us.Version, "ship", 0, 0, "TRACK-TEST", "", "", 1);
    Check((await Order(usNumber)).Status == OrderStatus.Shipped, "paid order can be fulfilled");
    Check(await Stock(2) == 1, "fulfillment does not decrement inventory a second time");
    await using (var db = new CatalogDbContext(options)) { db.Products.Remove(await db.Products.SingleAsync()); await db.SaveChangesAsync(); }
    Check((await Order(usNumber)).Items.Single().ProductName == "Test supplement", "historical order survives catalog deletion");
    Console.WriteLine($"SUCCESS: {checks} commerce checks passed. No live providers contacted.");
}
finally
{
    File.Delete(file);
}

sealed class TestFactory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext() => new(options);
}

sealed class TestAuth : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "admin@example.com")], "test"))));
    }
}

sealed class TestAuthorization : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) => Task.FromResult(AuthorizationResult.Success());
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) => Task.FromResult(AuthorizationResult.Success());
}

sealed class FakeGateway : HttpMessageHandler, IHttpClientFactory
{
    private string Reference = "";
    private long Amount;
    private string Currency = "";
    public long? AmountOverride;
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object payload;
        var stripe = request.RequestUri!.Host == "api.stripe.com";
        if (request.RequestUri.AbsolutePath == "/v1/tax/calculations")
        {
            var form = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (form["customer_details[address][state]"] != "MD" || form["shipping_cost[amount]"] != "500"
                || form["line_items[0][tax_code]"] != "txcd_99999999") throw new Exception("Incorrect tax request address, shipping, or classification.");
            payload = new { id = "taxcalc_test", tax_amount_exclusive = 462 };
        }
        else if (request.Method == HttpMethod.Post && stripe)
        {
            var form = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
            Reference = form["client_reference_id"].ToString(); Amount = long.Parse(form["line_items[0][price_data][unit_amount]"]!); Currency = "usd";
            payload = new { id = "cs_test", url = "https://checkout.stripe.com/test" };
        }
        else if (request.Method == HttpMethod.Post)
        {
            using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Reference = input.RootElement.GetProperty("reference").GetString()!; Amount = input.RootElement.GetProperty("amount").GetInt64(); Currency = "GHS";
            payload = new { status = true, data = new { authorization_url = "https://checkout.paystack.com/test" } };
        }
        else if (stripe) payload = new { payment_status = "paid", client_reference_id = Reference, amount_total = AmountOverride ?? Amount, currency = Currency };
        else payload = new { status = true, data = new { status = "success", reference = Reference, amount = AmountOverride ?? Amount, currency = Currency } };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) };
    }
}
