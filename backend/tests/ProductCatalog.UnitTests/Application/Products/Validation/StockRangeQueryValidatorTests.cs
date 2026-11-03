using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Validation;

namespace ProductCatalog.UnitTests.Application.Products.Validation;

public sealed class StockRangeQueryValidatorTests
{
    private readonly StockRangeQueryValidator _validator = new();

    [Theory]
    [InlineData(0, 10)]
    [InlineData(5, 5)]
    [InlineData(null, 10)]
    [InlineData(3, null)]
    public void ValidRanges_Pass(int? min, int? max)
    {
        Assert.True(_validator.Validate(new StockRangeQuery(min, max)).IsValid);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(-1, 10)]
    [InlineData(0, -1)]
    [InlineData(10, 1)]
    public void InvalidRanges_Fail(int? min, int? max)
    {
        Assert.False(_validator.Validate(new StockRangeQuery(min, max)).IsValid);
    }
}
