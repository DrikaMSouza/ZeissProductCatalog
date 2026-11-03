using ProductCatalog.Domain.Categories;
using ProductCatalog.Domain.Exceptions;

namespace ProductCatalog.Domain.Products;

/// <summary>
/// Aggregate root for a product of the catalogue. All state changes go through
/// behaviour methods so that business invariants (e.g. non-negative stock) are always enforced.
/// </summary>
public sealed class Product
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 1000;
    public const int SkuMaxLength = 32;

    // Required by EF Core.
    private Product()
    {
        Name = string.Empty;
        Sku = string.Empty;
    }

    private Product(int id, string sku, string name, string? description, decimal price, int stockQuantity, int categoryId, DateTime nowUtc)
    {
        if (!ProductId.IsValid(id))
        {
            throw new DomainValidationException($"Product id '{id}' must be a 6-digit number.");
        }

        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        EnsureInvariants();
    }

    public int Id { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int StockQuantity { get; private set; }

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token, protects stock updates from lost writes across instances.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public static Product Create(int id, string sku, string name, string? description, decimal price, int stockQuantity, int categoryId, DateTime nowUtc)
        => new(id, NormalizeSku(sku), name.Trim(), Normalize(description), price, stockQuantity, categoryId, nowUtc);

    /// <summary>SKUs are stored upper-case so that uniqueness does not depend on the database collation.</summary>
    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    public void Update(string sku, string name, string? description, decimal price, int stockQuantity, int categoryId, DateTime nowUtc)
    {
        Sku = NormalizeSku(sku);
        Name = name.Trim();
        Description = Normalize(description);
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        UpdatedAtUtc = nowUtc;

        EnsureInvariants();
    }

    public void DecrementStock(int quantity, DateTime nowUtc)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(Id, StockQuantity, quantity);
        }

        StockQuantity -= quantity;
        UpdatedAtUtc = nowUtc;
    }

    public void AddToStock(int quantity, DateTime nowUtc)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > int.MaxValue - StockQuantity)
        {
            throw new DomainValidationException("The resulting stock quantity exceeds the maximum allowed value.");
        }

        StockQuantity += quantity;
        UpdatedAtUtc = nowUtc;
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Quantity must be greater than zero.");
        }
    }

    private void EnsureInvariants()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new DomainValidationException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(Sku))
        {
            throw new DomainValidationException("Product SKU is required.");
        }

        if (Price < 0)
        {
            throw new DomainValidationException("Product price cannot be negative.");
        }

        if (StockQuantity < 0)
        {
            throw new DomainValidationException("Stock quantity cannot be negative.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
