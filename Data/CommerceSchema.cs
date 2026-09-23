using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class CommerceSchema
{
    public static async Task EnsureAsync(CatalogDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ShoppingCarts (Id TEXT NOT NULL PRIMARY KEY, Revision INTEGER NOT NULL, UpdatedUtc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS ShoppingCartItems (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, CartId TEXT NOT NULL REFERENCES ShoppingCarts(Id) ON DELETE CASCADE,
                VariantId INTEGER NOT NULL, Quantity INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_ShoppingCartItems_CartId_VariantId ON ShoppingCartItems(CartId, VariantId);
            CREATE TABLE IF NOT EXISTS CommerceOrders (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Number TEXT NOT NULL, CartId TEXT NOT NULL, CheckoutKey TEXT NOT NULL,
                Currency TEXT NOT NULL, Status INTEGER NOT NULL, CreatedUtc TEXT NOT NULL, CustomerName TEXT NOT NULL,
                Email TEXT NOT NULL, Phone TEXT NOT NULL, Address TEXT NOT NULL, City TEXT NOT NULL, Region TEXT NOT NULL,
                PostalCode TEXT NOT NULL, Country TEXT NOT NULL, Notes TEXT NOT NULL, Subtotal TEXT NOT NULL,
                Shipping TEXT NULL, Tax TEXT NULL, Total TEXT NULL, PaymentReference TEXT NOT NULL, TrackingReference TEXT NOT NULL,
                PaymentInstructions TEXT NOT NULL, Version TEXT NOT NULL, TaxEvidence TEXT NOT NULL,
                PaymentCurrency TEXT NOT NULL, ExchangeRate TEXT NOT NULL, PaymentAmount TEXT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_CommerceOrders_Number ON CommerceOrders(Number);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_CommerceOrders_CheckoutKey ON CommerceOrders(CheckoutKey);
            CREATE INDEX IF NOT EXISTS IX_CommerceOrders_CartId ON CommerceOrders(CartId);
            CREATE TABLE IF NOT EXISTS CommerceOrderItems (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, OrderId INTEGER NOT NULL REFERENCES CommerceOrders(Id) ON DELETE CASCADE,
                VariantId INTEGER NOT NULL, ProductName TEXT NOT NULL, VariantName TEXT NOT NULL, Sku TEXT NOT NULL,
                UnitPrice TEXT NOT NULL, Quantity INTEGER NOT NULL, StockReserved INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_CommerceOrderItems_OrderId ON CommerceOrderItems(OrderId);
            CREATE TABLE IF NOT EXISTS CommerceOrderEvents (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, OrderId INTEGER NOT NULL REFERENCES CommerceOrders(Id) ON DELETE CASCADE,
                CreatedUtc TEXT NOT NULL, Actor TEXT NOT NULL, Description TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_CommerceOrderEvents_OrderId ON CommerceOrderEvents(OrderId);
            CREATE TABLE IF NOT EXISTS PaymentAttempts (
                Id TEXT NOT NULL PRIMARY KEY, OrderId INTEGER NOT NULL REFERENCES CommerceOrders(Id) ON DELETE RESTRICT,
                Provider TEXT NOT NULL, ProviderId TEXT NOT NULL, CheckoutUrl TEXT NOT NULL, AmountMinor INTEGER NOT NULL,
                Currency TEXT NOT NULL, Paid INTEGER NOT NULL, CreatedUtc TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentAttempts_OrderId ON PaymentAttempts(OrderId);
            """);
    }
}
