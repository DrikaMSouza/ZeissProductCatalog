using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Mapping;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Services;

public sealed class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IProductIdGenerator idGenerator,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    IValidator<StockRangeQuery> stockRangeValidator,
    TimeProvider timeProvider,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken)
        => (await products.GetAllAsync(cancellationToken)).ToResponse();

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        => (await GetExistingAsync(id, cancellationToken)).ToResponse();

    public async Task<IReadOnlyList<ProductResponse>> SearchByNameAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException([new ValidationFailure("name", "A search term is required.")]);
        }

        return (await products.SearchByNameAsync(name.Trim(), cancellationToken)).ToResponse();
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByStockRangeAsync(StockRangeQuery query, CancellationToken cancellationToken)
    {
        await stockRangeValidator.ValidateAndThrowAsync(query, cancellationToken);

        return (await products.GetByStockRangeAsync(query.Min, query.Max, cancellationToken)).ToResponse();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSkuIsUniqueAsync(request.Sku, excludingProductId: null, cancellationToken);

        var id = await idGenerator.NextIdAsync(cancellationToken);
        var product = Product.Create(
            id,
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            timeProvider.GetUtcNow().UtcDateTime);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} ({Sku}) created", product.Id, product.Sku);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = await GetExistingAsync(id, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSkuIsUniqueAsync(request.Sku, excludingProductId: id, cancellationToken);

        product.Update(
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} updated", product.Id);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await GetExistingAsync(id, cancellationToken);

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} deleted", id);
    }

    private async Task<Product> GetExistingAsync(int id, CancellationToken cancellationToken)
        => await products.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(Product), id);

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await categories.ExistsAsync(categoryId, cancellationToken))
        {
            throw new ValidationException([new ValidationFailure(nameof(CreateProductRequest.CategoryId), $"Category '{categoryId}' does not exist.")]);
        }
    }

    private async Task EnsureSkuIsUniqueAsync(string sku, int? excludingProductId, CancellationToken cancellationToken)
    {
        var normalizedSku = Product.NormalizeSku(sku);

        if (await products.SkuExistsAsync(normalizedSku, excludingProductId, cancellationToken))
        {
            throw new ConflictException($"A product with SKU '{normalizedSku}' already exists.");
        }
    }
}
