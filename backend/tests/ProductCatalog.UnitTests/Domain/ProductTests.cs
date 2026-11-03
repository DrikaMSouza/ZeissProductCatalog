using ProductCatalog.Domain.Exceptions;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.UnitTests.Domain;

public sealed class ProductTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Product CreateProduct(int stock = 10)
        => Product.Create(123456, " lns-01 ", "  Lens  ", "  ", 10m, stock, 1, Now);

    [Fact]
    public void Create_NormalizesTextFields()
    {
        var product = CreateProduct();

        Assert.Equal("LNS-01", product.Sku);
        Assert.Equal("Lens", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(Now, product.CreatedAtUtc);
        Assert.Equal(Now, product.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    [InlineData(0)]
    public void Create_WithIdOutsideSixDigitRange_Throws(int id)
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(id, "SKU", "Lens", null, 1m, 1, 1, Now));
    }

    [Fact]
    public void Create_WithNegativeStock_Throws()
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(123456, "SKU", "Lens", null, 1m, -1, 1, Now));
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(123456, "SKU", "Lens", null, -1m, 1, 1, Now));
    }

    [Fact]
    public void DecrementStock_RemovesQuantityAndTouchesUpdatedAt()
    {
        var product = CreateProduct(stock: 10);
        var later = Now.AddMinutes(5);

        product.DecrementStock(4, later);

        Assert.Equal(6, product.StockQuantity);
        Assert.Equal(later, product.UpdatedAtUtc);
    }

    [Fact]
    public void DecrementStock_ToExactlyZero_IsAllowed()
    {
        var product = CreateProduct(stock: 3);

        product.DecrementStock(3, Now);

        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void DecrementStock_MoreThanAvailable_ThrowsAndKeepsStock()
    {
        var product = CreateProduct(stock: 2);

        var exception = Assert.Throws<InsufficientStockException>(() => product.DecrementStock(5, Now));

        Assert.Equal(2, exception.AvailableStock);
        Assert.Equal(5, exception.RequestedQuantity);
        Assert.Equal(2, product.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void StockMovements_WithNonPositiveQuantity_Throw(int quantity)
    {
        var product = CreateProduct();

        Assert.Throws<DomainValidationException>(() => product.DecrementStock(quantity, Now));
        Assert.Throws<DomainValidationException>(() => product.AddToStock(quantity, Now));
    }

    [Fact]
    public void AddToStock_IncreasesQuantity()
    {
        var product = CreateProduct(stock: 1);

        product.AddToStock(9, Now);

        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void AddToStock_Overflow_Throws()
    {
        var product = CreateProduct(stock: int.MaxValue - 1);

        Assert.Throws<DomainValidationException>(() => product.AddToStock(2, Now));
    }

    [Fact]
    public void Update_ReplacesValues()
    {
        var product = CreateProduct();
        var later = Now.AddDays(1);

        product.Update("new-sku", "New name", "Desc", 25m, 3, 2, later);

        Assert.Equal("NEW-SKU", product.Sku);
        Assert.Equal("New name", product.Name);
        Assert.Equal("Desc", product.Description);
        Assert.Equal(25m, product.Price);
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(2, product.CategoryId);
        Assert.Equal(later, product.UpdatedAtUtc);
        Assert.Equal(Now, product.CreatedAtUtc);
    }
}
