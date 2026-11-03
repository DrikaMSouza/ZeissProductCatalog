using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Services;

public interface IProductStockService
{
    Task<ProductResponse> DecrementStockAsync(int productId, int quantity, CancellationToken cancellationToken);

    Task<ProductResponse> AddToStockAsync(int productId, int quantity, CancellationToken cancellationToken);
}
