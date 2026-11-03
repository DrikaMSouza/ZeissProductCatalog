namespace ProductCatalog.Domain.Products;

/// <summary>
/// Business rules for product identifiers: a unique 6-digit number (100000 - 999999).
/// </summary>
public static class ProductId
{
    public const int MinValue = 100_000;
    public const int MaxValue = 999_999;

    public static bool IsValid(int id) => id is >= MinValue and <= MaxValue;
}
