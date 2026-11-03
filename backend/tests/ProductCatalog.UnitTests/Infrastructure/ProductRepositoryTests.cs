using ProductCatalog.Domain.Products;
using ProductCatalog.Infrastructure.Persistence.Repositories;

namespace ProductCatalog.UnitTests.Infrastructure;

public sealed class ProductRepositoryTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    public ProductRepositoryTests()
    {
        using var context = _database.CreateContext();
        context.Products.AddRange(
            Product.Create(100001, "LNS-1", "Progressive Lens", null, 300m, 0, 1, Now),
            Product.Create(100002, "LNS-2", "Single Vision LENS", null, 90m, 5, 1, Now),
            Product.Create(100003, "FRM-1", "Titanium Frame", null, 200m, 50, 2, Now));
        context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProductsWithCategoryAndStock_OrderedByName()
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        var products = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(["Progressive Lens", "Single Vision LENS", "Titanium Frame"], products.Select(p => p.Name));
        Assert.All(products, p => Assert.NotNull(p.Category));
        Assert.Equal([0, 5, 50], products.Select(p => p.StockQuantity));
    }

    [Theory]
    [InlineData("lens", 2)]
    [InlineData("LENS", 2)]
    [InlineData("vision", 1)]
    [InlineData("ium fr", 1)]
    [InlineData("unknown", 0)]
    public async Task SearchByNameAsync_MatchesPartiallyAndCaseInsensitively(string term, int expectedCount)
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        var products = await repository.SearchByNameAsync(term, CancellationToken.None);

        Assert.Equal(expectedCount, products.Count);
    }

    [Theory]
    [InlineData(0, 5, new[] { 100001, 100002 })]
    [InlineData(5, 50, new[] { 100002, 100003 })]
    [InlineData(1, 4, new int[0])]
    [InlineData(null, 0, new[] { 100001 })]
    [InlineData(6, null, new[] { 100003 })]
    public async Task GetByStockRangeAsync_IsInclusive(int? min, int? max, int[] expectedIds)
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        var products = await repository.GetByStockRangeAsync(min, max, CancellationToken.None);

        Assert.Equal(expectedIds, products.Select(p => p.Id));
    }

    [Fact]
    public async Task SkuExistsAsync_CanExcludeTheProductBeingUpdated()
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        Assert.True(await repository.SkuExistsAsync("LNS-1", null, CancellationToken.None));
        Assert.False(await repository.SkuExistsAsync("LNS-1", 100001, CancellationToken.None));
        Assert.True(await repository.SkuExistsAsync("LNS-1", 100002, CancellationToken.None));
        Assert.False(await repository.SkuExistsAsync("NOPE", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTrackedEntity_SoChangesCanBeSaved()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var product = await repository.GetByIdAsync(100002, CancellationToken.None);

            product!.DecrementStock(2, Now);
            await context.SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        Assert.Equal(3, (await verification.Products.FindAsync(100002))!.StockQuantity);
    }

    [Fact]
    public async Task Remove_DeletesProduct()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new ProductRepository(context);
            repository.Remove((await repository.GetByIdAsync(100003, CancellationToken.None))!);
            await context.SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        Assert.Null(await verification.Products.FindAsync(100003));
    }
}
