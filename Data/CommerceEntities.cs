#nullable enable
using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class ShoppingCart
{
    public string Id { get; set; } = "";
    public int Revision { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<ShoppingCartItem> Items { get; set; } = [];
}

public sealed class ShoppingCartItem
{
    public int Id { get; set; }
    public string CartId { get; set; } = "";
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus { QuoteRequested, AwaitingPayment, Paid, Shipped, Cancelled }

public sealed class CommerceOrder
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string CartId { get; set; } = "";
    public string CheckoutKey { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public OrderStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal? Shipping { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Total { get; set; }
    public string PaymentReference { get; set; } = "";
    public string TrackingReference { get; set; } = "";
    public string PaymentInstructions { get; set; } = "";
    public string TaxEvidence { get; set; } = "";
    public string PaymentCurrency { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal? PaymentAmount { get; set; }
    public string Version { get; set; } = Guid.NewGuid().ToString("N");
    public List<CommerceOrderItem> Items { get; set; } = [];
    public List<CommerceOrderEvent> Events { get; set; } = [];
}

public sealed class PaymentAttempt
{
    public string Id { get; set; } = "";
    public int OrderId { get; set; }
    public string Provider { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "";
    public bool Paid { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CommerceOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    // Deliberately no variant foreign key: purchase snapshots survive catalog deletion.
    public int VariantId { get; set; }
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool StockReserved { get; set; }
}

public sealed class CommerceOrderEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string Actor { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class CheckoutInput
{
    [Required, StringLength(140)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(40)] public string Phone { get; set; } = "";
    [Required, StringLength(250)] public string Address { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(100)] public string Region { get; set; } = "";
    [StringLength(30)] public string PostalCode { get; set; } = "";
    [Required, RegularExpression("^(GH|US)$")] public string Country { get; set; } = "GH";
    [StringLength(2000)] public string Notes { get; set; } = "";
}
