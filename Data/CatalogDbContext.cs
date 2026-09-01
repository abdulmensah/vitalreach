using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<HeadquartersSettings> Headquarters => Set<HeadquartersSettings>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<ProductEntity>().Property(x => x.Price).HasConversion<double>();
        modelBuilder.Entity<ProductImage>()
            .HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductImage>().HasIndex(image => new { image.ProductId, image.ImageUrl }).IsUnique();
        modelBuilder.Entity<AdminUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        modelBuilder.Entity<HeadquartersSettings>().HasKey(x => x.Id);
    }
}
