using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Testing;

public static class TestRequests
{
    /// <summary>Category seeded by the migration ("Spectacle Lenses").</summary>
    public const int ExistingCategoryId = 1;

    public static CreateProductRequest CreateProduct(
        string? sku = null,
        string name = "Test Lens",
        decimal price = 99.90m,
        int stockQuantity = 10,
        int categoryId = ExistingCategoryId)
        => new(sku ?? $"SKU-{Guid.NewGuid():N}"[..20], name, "Created by an automated test", price, stockQuantity, categoryId);

    public static UpdateProductRequest UpdateProduct(
        string sku,
        string name = "Updated Lens",
        decimal price = 120m,
        int stockQuantity = 20,
        int categoryId = ExistingCategoryId)
        => new(sku, name, "Updated by an automated test", price, stockQuantity, categoryId);
}
