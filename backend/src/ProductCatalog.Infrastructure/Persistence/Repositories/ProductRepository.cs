using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(ProductCatalogDbContext dbContext) : IProductRepository
{
    private IQueryable<Product> ReadOnlyProducts => dbContext.Products
        .AsNoTracking()
        .Include(p => p.Category);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
        => await ReadOnlyProducts
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Products
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken)
    {
        // Explicit case-insensitive match so that behaviour does not depend on the database collation.
        var term = name.ToUpper();

        return await ReadOnlyProducts
            .Where(p => p.Name.ToUpper().Contains(term))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByStockRangeAsync(int? min, int? max, CancellationToken cancellationToken)
    {
        var query = ReadOnlyProducts;

        if (min.HasValue)
        {
            query = query.Where(p => p.StockQuantity >= min.Value);
        }

        if (max.HasValue)
        {
            query = query.Where(p => p.StockQuantity <= max.Value);
        }

        return await query
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SkuExistsAsync(string sku, int? excludingProductId, CancellationToken cancellationToken)
        => dbContext.Products.AnyAsync(
            p => p.Sku == sku && (excludingProductId == null || p.Id != excludingProductId),
            cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);

    public void Remove(Product product) => dbContext.Products.Remove(product);
}
