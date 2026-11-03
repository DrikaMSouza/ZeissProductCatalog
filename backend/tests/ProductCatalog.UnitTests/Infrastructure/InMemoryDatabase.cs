using Microsoft.EntityFrameworkCore;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.UnitTests.Infrastructure;

/// <summary>Creates DbContexts that share one isolated EF Core InMemory database per test.</summary>
internal sealed class InMemoryDatabase
{
    private readonly DbContextOptions<ProductCatalogDbContext> _options = new DbContextOptionsBuilder<ProductCatalogDbContext>()
        .UseInMemoryDatabase($"unit-tests-{Guid.NewGuid():N}")
        .Options;

    /// <summary>Returns a new context; the first call also creates the schema and seeds the reference categories.</summary>
    public ProductCatalogDbContext CreateContext()
    {
        var context = new ProductCatalogDbContext(_options);
        context.Database.EnsureCreated();
        return context;
    }
}
