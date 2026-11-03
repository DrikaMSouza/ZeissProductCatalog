namespace ProductCatalog.Application.Products.Contracts;

public sealed record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
