using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<HeadquartersSettings> Headquarters => Set<HeadquartersSettings>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<ProductEntity>().Property(x => x.Price).HasConversion<double>();
        modelBuilder.Entity<AdminUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        modelBuilder.Entity<HeadquartersSettings>().HasKey(x => x.Id);
    }
}
