using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Persistence.Seeding;

namespace ProductCatalog.UnitTests.Infrastructure;

public sealed class DatabaseInitializerTests
{
    private readonly InMemoryDatabase _database = new();
    private readonly IProductIdGenerator _idGenerator = Substitute.For<IProductIdGenerator>();

    public DatabaseInitializerTests()
    {
        var next = 200000;
        _idGenerator.NextIdAsync(Arg.Any<CancellationToken>()).Returns(_ => next++);
    }

    private DatabaseInitializer CreateInitializer(ProductCatalogDbContext context, bool seed = true) => new(
        context,
        _idGenerator,
        new FakeTimeProvider(),
        Options.Create(new DatabaseOptions { ApplyMigrationsOnStartup = true, SeedSampleData = seed }),
        NullLogger<DatabaseInitializer>.Instance);

    [Fact]
    public async Task InitializeAsync_SeedsCategoriesAndSampleProducts()
    {
        await using var context = _database.CreateContext();

        await CreateInitializer(context).InitializeAsync();

        Assert.Equal(CatalogSeedData.Categories.Length, await context.Categories.CountAsync());
        Assert.Equal(CatalogSeedData.Products.Length, await context.Products.CountAsync());
        Assert.All(await context.Products.ToListAsync(), p => Assert.InRange(p.Id, 200000, 200000 + CatalogSeedData.Products.Length - 1));
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        await using (var context = _database.CreateContext())
        {
            await CreateInitializer(context).InitializeAsync();
        }

        await using (var context = _database.CreateContext())
        {
            await CreateInitializer(context).InitializeAsync();
            Assert.Equal(CatalogSeedData.Products.Length, await context.Products.CountAsync());
        }

        await _idGenerator.Received(CatalogSeedData.Products.Length).NextIdAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_WithSeedingDisabled_LeavesProductsEmpty()
    {
        await using var context = _database.CreateContext();

        await CreateInitializer(context, seed: false).InitializeAsync();

        Assert.Empty(await context.Products.ToListAsync());
    }
}
