using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Categories;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence;

public sealed class ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";
    public const string ProductIdSequence = "ProductIds";

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Single source of truth for product ids, shared by every API instance.
        modelBuilder.HasSequence<int>(ProductIdSequence)
            .StartsAt(ProductId.MinValue)
            .IncrementsBy(1)
            .HasMin(ProductId.MinValue)
            .HasMax(ProductId.MaxValue)
            .IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);
    }
}
