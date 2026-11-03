using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> SearchByNameAsync(string? name, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> GetByStockRangeAsync(StockRangeQuery query, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
