using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<HeadquartersSettings> Headquarters => Set<HeadquartersSettings>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    public DbSet<CommerceOrder> CommerceOrders => Set<CommerceOrder>();
    public DbSet<CommerceOrderItem> CommerceOrderItems => Set<CommerceOrderItem>();
    public DbSet<CommerceOrderEvent> CommerceOrderEvents => Set<CommerceOrderEvent>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<ProductEntity>().Property(x => x.Price).HasConversion<double>();
        modelBuilder.Entity<ProductVariant>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<ProductVariant>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<ProductVariant>().Property(x => x.Sku).UseCollation("NOCASE");
        modelBuilder.Entity<ProductVariant>().HasOne<ProductEntity>().WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductImage>()
            .HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductImage>().HasIndex(image => new { image.ProductId, image.ImageUrl }).IsUnique();
        modelBuilder.Entity<AdminUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        modelBuilder.Entity<HeadquartersSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<ShoppingCart>().Property(x => x.Revision).IsConcurrencyToken();
        modelBuilder.Entity<ShoppingCartItem>().HasOne<ShoppingCart>().WithMany(x => x.Items).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ShoppingCartItem>().HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();
        modelBuilder.Entity<CommerceOrder>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<CommerceOrder>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<CommerceOrder>().HasIndex(x => x.CheckoutKey).IsUnique();
        modelBuilder.Entity<CommerceOrder>().HasIndex(x => x.CartId);
        modelBuilder.Entity<PaymentAttempt>().HasIndex(x => x.OrderId).IsUnique();
        modelBuilder.Entity<PaymentAttempt>().HasOne<CommerceOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CommerceOrderItem>().HasOne<CommerceOrder>().WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CommerceOrderEvent>().HasOne<CommerceOrder>().WithMany(x => x.Events).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
