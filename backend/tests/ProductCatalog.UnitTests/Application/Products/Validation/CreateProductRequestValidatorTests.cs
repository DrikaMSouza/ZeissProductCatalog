using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Validation;

namespace ProductCatalog.UnitTests.Application.Products.Validation;

public sealed class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequest Valid() => new("LNS-001", "Lens", null, 10.50m, 0, 1);

    [Fact]
    public void ValidRequest_Passes()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    public static TheoryData<CreateProductRequest, string> InvalidRequests => new()
    {
        { Valid() with { Sku = "" }, nameof(CreateProductRequest.Sku) },
        { Valid() with { Sku = "has spaces" }, nameof(CreateProductRequest.Sku) },
        { Valid() with { Sku = new string('A', 33) }, nameof(CreateProductRequest.Sku) },
        { Valid() with { Name = " " }, nameof(CreateProductRequest.Name) },
        { Valid() with { Name = new string('a', 151) }, nameof(CreateProductRequest.Name) },
        { Valid() with { Description = new string('a', 1001) }, nameof(CreateProductRequest.Description) },
        { Valid() with { Price = 0m }, nameof(CreateProductRequest.Price) },
        { Valid() with { Price = 1.999m }, nameof(CreateProductRequest.Price) },
        { Valid() with { Price = 1_000_000.01m }, nameof(CreateProductRequest.Price) },
        { Valid() with { StockQuantity = -1 }, nameof(CreateProductRequest.StockQuantity) },
        { Valid() with { CategoryId = 0 }, nameof(CreateProductRequest.CategoryId) },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidRequest_FailsOnExpectedProperty(CreateProductRequest request, string expectedProperty)
    {
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == expectedProperty);
    }
}
