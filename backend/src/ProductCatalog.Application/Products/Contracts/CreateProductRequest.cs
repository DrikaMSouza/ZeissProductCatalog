namespace ProductCatalog.Application.Products.Contracts;

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId) : IProductRequest;
