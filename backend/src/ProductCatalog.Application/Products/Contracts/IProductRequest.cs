namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Shared shape of the create and update payloads, so both share one set of validation rules.</summary>
public interface IProductRequest
{
    string Sku { get; }

    string Name { get; }

    string? Description { get; }

    decimal Price { get; }

    int StockQuantity { get; }

    int CategoryId { get; }
}
