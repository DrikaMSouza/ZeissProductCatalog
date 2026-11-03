using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByStockRangeAsync(int? min, int? max, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, int? excludingProductId, CancellationToken cancellationToken);

    void Add(Product product);

    void Remove(Product product);
}
