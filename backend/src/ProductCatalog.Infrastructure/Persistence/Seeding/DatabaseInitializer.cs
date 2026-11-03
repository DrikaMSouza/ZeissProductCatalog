using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Seeding;

/// <summary>Applies pending migrations and seeds sample products on an empty database.</summary>
public sealed class DatabaseInitializer(
    ProductCatalogDbContext dbContext,
    IProductIdGenerator idGenerator,
    TimeProvider timeProvider,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (options.Value.ApplyMigrationsOnStartup)
        {
            if (dbContext.Database.IsRelational())
            {
                logger.LogInformation("Applying database migrations");
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        if (options.Value.SeedSampleData)
        {
            await SeedProductsAsync(cancellationToken);
        }
    }

    public async Task SeedProductsAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var sample in CatalogSeedData.Products)
        {
            var id = await idGenerator.NextIdAsync(cancellationToken);
            dbContext.Products.Add(Product.Create(
                id,
                sample.Sku,
                sample.Name,
                sample.Description,
                sample.Price,
                sample.StockQuantity,
                sample.CategoryId,
                nowUtc));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} sample products", CatalogSeedData.Products.Length);
        }
        catch (DbUpdateException ex)
        {
            // Another instance seeded the database at the same time (unique SKU index); nothing to do.
            logger.LogWarning(ex, "Sample products were not seeded because they already exist");
            dbContext.ChangeTracker.Clear();
        }
    }
}
