using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Mapping;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Services;

/// <summary>
/// Stock movements. Concurrent movements on the same product (also from other instances)
/// are protected by the product's optimistic concurrency token.
/// </summary>
public sealed class ProductStockService(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProductStockService> logger) : IProductStockService
{
    public Task<ProductResponse> DecrementStockAsync(int productId, int quantity, CancellationToken cancellationToken)
        => ApplyAsync(productId, product => product.DecrementStock(quantity, timeProvider.GetUtcNow().UtcDateTime), -quantity, cancellationToken);

    public Task<ProductResponse> AddToStockAsync(int productId, int quantity, CancellationToken cancellationToken)
        => ApplyAsync(productId, product => product.AddToStock(quantity, timeProvider.GetUtcNow().UtcDateTime), quantity, cancellationToken);

    private async Task<ProductResponse> ApplyAsync(int productId, Action<Product> movement, int delta, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Product), productId);

        movement(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stock of product {ProductId} changed by {Delta}; new stock {StockQuantity}",
            product.Id,
            delta,
            product.StockQuantity);

        return product.ToResponse();
    }
}
