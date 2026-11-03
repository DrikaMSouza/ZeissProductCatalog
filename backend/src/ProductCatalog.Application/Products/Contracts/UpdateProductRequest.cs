namespace ProductCatalog.Application.Products.Contracts;

public sealed record UpdateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId) : IProductRequest;
