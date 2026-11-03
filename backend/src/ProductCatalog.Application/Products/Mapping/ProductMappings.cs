using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Mapping;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CategoryId,
        product.Category?.Name,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);

    public static IReadOnlyList<ProductResponse> ToResponse(this IEnumerable<Product> products)
        => products.Select(ToResponse).ToList();
}
