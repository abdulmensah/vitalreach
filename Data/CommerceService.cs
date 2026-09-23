using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed record CartRow(int VariantId, string ProductName, string VariantName, string Sku,
    decimal UnitPrice, int Quantity, bool Available, int? StockQuantity);
public sealed record CartView(int Revision, List<CartRow> Items, string Fingerprint)
{
    public decimal Subtotal => Items.Sum(x => x.UnitPrice * x.Quantity);
    public bool CanCheckout => Items.Count > 0 && Items.All(x => x.Available);
}

public sealed class CommerceService(IDbContextFactory<CatalogDbContext> factory, CartSession session,
    AuthenticationStateProvider authentication, IAuthorizationService authorization)
{
    private string CartId => string.IsNullOrEmpty(session.Id)
        ? throw new InvalidOperationException("Your shopping session could not be loaded. Please refresh the page with cookies enabled.") : session.Id;

    public async Task<CartView> GetCartAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var cart = await db.ShoppingCarts.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == CartId);
        return await ViewAsync(db, cart);
    }

    private static async Task<CartView> ViewAsync(CatalogDbContext db, ShoppingCart? cart)
    {
        var rows = new List<CartRow>();
        if (cart is not null)
        {
            var ids = cart.Items.Select(x => x.VariantId).ToArray();
            var products = await db.Products.AsNoTracking().Include(x => x.Variants)
                .Where(p => p.Variants.Any(v => ids.Contains(v.Id))).ToListAsync();
            foreach (var item in cart.Items.OrderBy(x => x.VariantId))
            {
                var product = products.FirstOrDefault(p => p.Variants.Any(v => v.Id == item.VariantId));
                var variant = product?.Variants.Single(v => v.Id == item.VariantId);
                rows.Add(new(item.VariantId, product?.Name ?? "Removed product", variant?.DisplayName ?? "Unavailable variant",
                    variant?.Sku ?? "", variant?.Price ?? 0, item.Quantity,
                    product?.IsPublished == true && variant?.CanOrder == true && (!variant.StockQuantity.HasValue || variant.StockQuantity >= item.Quantity),
                    variant?.StockQuantity));
            }
        }
        var signature = string.Join("|", rows.Select(r => $"{r.VariantId}:{r.Quantity}:{r.UnitPrice.ToString(CultureInfo.InvariantCulture)}:{r.Available}:{r.Sku}:{r.VariantName}:{r.ProductName}"));
        return new(cart?.Revision ?? 0, rows, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature))));
    }

    public async Task AddAsync(int variantId, int quantity = 1)
        => await ChangeAsync(variantId, quantity, true);
    public async Task SetQuantityAsync(int variantId, int quantity)
        => await ChangeAsync(variantId, quantity, false);

    private async Task ChangeAsync(int variantId, int quantity, bool add)
    {
        if (quantity < 0 || quantity > 99 || (add && quantity == 0))
            throw new InvalidOperationException("Choose a quantity between 1 and 99.");
        await using var db = await factory.CreateDbContextAsync();
        var cart = await db.ShoppingCarts.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == CartId);
        if (cart is null) { cart = new ShoppingCart { Id = CartId }; db.ShoppingCarts.Add(cart); }
        var item = cart.Items.SingleOrDefault(x => x.VariantId == variantId);
        var desired = add ? (item?.Quantity ?? 0) + quantity : quantity;
        if (desired > 0)
        {
            if (cart.Items.Count >= 50 && item is null) throw new InvalidOperationException("Your bag can contain at most 50 different variants.");
            var variant = await db.ProductVariants.SingleOrDefaultAsync(v => v.Id == variantId);
            if (variant is null || !variant.CanOrder || !await db.Products.AnyAsync(p => p.Id == variant.ProductId && p.IsPublished))
                throw new InvalidOperationException("This variant is no longer available. Please remove it or choose another.");
            if (desired > 99 || (variant.StockQuantity.HasValue && desired > variant.StockQuantity))
                throw new InvalidOperationException("The requested quantity exceeds the available stock or the 99-item limit.");
            if (item is null) cart.Items.Add(new ShoppingCartItem { VariantId = variantId, Quantity = desired });
            else item.Quantity = desired;
        }
        else if (item is not null) db.ShoppingCartItems.Remove(item);
        cart.Revision++;
        cart.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<string> CheckoutAsync(CheckoutInput input, int expectedRevision, string expectedFingerprint)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (input.Country == "US" && string.IsNullOrWhiteSpace(input.PostalCode))
            throw new InvalidOperationException("Enter a ZIP code for US delivery.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var key = $"{CartId}:{expectedRevision}";
        var previous = await db.CommerceOrders.SingleOrDefaultAsync(o => o.CheckoutKey == key);
        if (previous is not null) return previous.Number;
        var cart = await db.ShoppingCarts.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == CartId);
        var view = await ViewAsync(db, cart);
        if (cart is null || !view.CanCheckout) throw new InvalidOperationException("Your bag is empty or contains unavailable items. Review it before checkout.");
        if (cart.Revision != expectedRevision || view.Fingerprint != expectedFingerprint)
            throw new InvalidOperationException("Your bag, prices, or availability changed. Review the refreshed bag and submit again.");
        var order = new CommerceOrder
        {
            Number = $"VR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..26].ToUpperInvariant(),
            CartId = CartId, CheckoutKey = key, CustomerName = input.Name.Trim(), Email = input.Email.Trim(),
            Phone = input.Phone.Trim(), Address = input.Address.Trim(), City = input.City.Trim(), Region = input.Region.Trim(),
            PostalCode = input.PostalCode.Trim(), Country = input.Country, Notes = input.Notes.Trim(), Subtotal = view.Subtotal,
            Items = view.Items.Select(r => new CommerceOrderItem { VariantId = r.VariantId, ProductName = r.ProductName,
                VariantName = r.VariantName, Sku = r.Sku, UnitPrice = r.UnitPrice, Quantity = r.Quantity }).ToList(),
            Events = [new CommerceOrderEvent { Actor = "Customer", Description = "Order request submitted. Shipping, tax, and payment require a confirmed quote." }]
        };
        db.CommerceOrders.Add(order);
        db.ShoppingCartItems.RemoveRange(cart.Items);
        cart.Revision++;
        cart.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order.Number;
    }

    public async Task<List<CommerceOrder>> MyOrdersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.CommerceOrders.AsNoTracking().Include(x => x.Items).Where(x => x.CartId == CartId)
            .OrderByDescending(x => x.CreatedUtc).Take(50).ToListAsync();
    }

    private async Task<string> RequireAdminAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (!(await authorization.AuthorizeAsync(user, null, "Admin")).Succeeded)
            throw new UnauthorizedAccessException("Administrator access is required.");
        return user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Administrator";
    }

    public async Task<List<CommerceOrder>> OrdersAsync()
    {
        await RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.CommerceOrders.AsNoTracking().Include(x => x.Items).Include(x => x.Events).OrderByDescending(x => x.CreatedUtc).ToListAsync();
    }

    public async Task UpdateOrderAsync(int id, string version, string action, decimal shipping, decimal tax, string reference, string instructions,
        string taxEvidence, decimal exchangeRate)
    {
        var actor = await RequireAdminAsync();
        if (reference.Length > 250 || instructions.Length > 2000 || taxEvidence.Length > 2000) throw new InvalidOperationException("The reference or instructions are too long.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.CommerceOrders.Include(x => x.Items).SingleAsync(x => x.Id == id);
        if (order.Version != version) throw new InvalidOperationException("This order was changed by another administrator. Refresh and try again.");
        switch (action)
        {
            case "quote" when order.Status == OrderStatus.QuoteRequested:
                if (string.IsNullOrWhiteSpace(taxEvidence)) throw new InvalidOperationException("Document the applicable tax calculation and registration review, including the reason for any zero tax.");
                if (exchangeRate <= 0 || exchangeRate > 100000 || (order.Country == "US" && exchangeRate != 1))
                    throw new InvalidOperationException("Enter the confirmed USD-to-GHS rate for Ghana, or 1 for US orders.");
                if (shipping < 0 || tax < 0 || shipping > 100000 || tax > 100000 || decimal.Round(shipping, 2) != shipping || decimal.Round(tax, 2) != tax)
                    throw new InvalidOperationException("Enter valid shipping and tax amounts with at most two decimal places.");
                if (string.IsNullOrWhiteSpace(instructions)) throw new InvalidOperationException("Enter payment instructions for the customer.");
                foreach (var line in order.Items)
                {
                    var variant = await db.ProductVariants.AsNoTracking().SingleOrDefaultAsync(v => v.Id == line.VariantId);
                    if (variant is null || !variant.CanOrder || !await db.Products.AnyAsync(p => p.Id == variant.ProductId && p.IsPublished))
                        throw new InvalidOperationException($"{line.Sku} is no longer available. Resolve availability before quoting.");
                    if (variant.StockQuantity.HasValue)
                    {
                        var changed = await db.ProductVariants.Where(v => v.Id == line.VariantId && v.IsAvailable && v.StockQuantity >= line.Quantity)
                            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - line.Quantity).SetProperty(v => v.Version, v => v.Version + 1));
                        if (changed != 1) throw new InvalidOperationException($"Not enough stock for {line.Sku}. The quote was not saved.");
                        line.StockReserved = true;
                    }
                }
                order.Shipping = shipping; order.Tax = tax;
                order.Total = order.Subtotal + shipping + tax;
                order.TaxEvidence = taxEvidence.Trim();
                order.ExchangeRate = exchangeRate;
                order.PaymentCurrency = order.Country == "GH" ? "GHS" : "USD";
                order.PaymentAmount = decimal.Round(order.Total.Value * exchangeRate, 2, MidpointRounding.AwayFromZero);
                order.PaymentInstructions = instructions.Trim();
                order.Status = OrderStatus.AwaitingPayment;
                break;
            case "paid" when order.Status == OrderStatus.AwaitingPayment:
                if (await db.PaymentAttempts.AnyAsync(p => p.OrderId == id)) throw new InvalidOperationException("A gateway payment is in progress. Verify it through the gateway rather than recording a manual payment.");
                if (string.IsNullOrWhiteSpace(reference)) throw new InvalidOperationException("Enter the verified payment reference before marking paid.");
                order.PaymentReference = reference.Trim(); order.Status = OrderStatus.Paid;
                break;
            case "ship" when order.Status == OrderStatus.Paid:
                if (string.IsNullOrWhiteSpace(reference)) throw new InvalidOperationException("Enter a carrier/tracking or collection reference.");
                order.TrackingReference = reference.Trim(); order.Status = OrderStatus.Shipped;
                break;
            case "cancel" when order.Status is OrderStatus.QuoteRequested or OrderStatus.AwaitingPayment:
                if (await db.PaymentAttempts.AnyAsync(p => p.OrderId == id)) throw new InvalidOperationException("A payment session exists. Resolve or expire it with the provider before cancellation; contact support for reconciliation.");
                foreach (var line in order.Items.Where(x => x.StockReserved))
                {
                    await db.ProductVariants.Where(v => v.Id == line.VariantId && v.StockQuantity != null)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + line.Quantity).SetProperty(v => v.Version, v => v.Version + 1));
                    line.StockReserved = false;
                }
                order.Status = OrderStatus.Cancelled;
                break;
            default: throw new InvalidOperationException("That action is not valid for the current order status.");
        }
        order.Version = Guid.NewGuid().ToString("N");
        db.CommerceOrderEvents.Add(new CommerceOrderEvent { OrderId = id, Actor = actor,
            Description = $"{action}: {order.Status}" });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
