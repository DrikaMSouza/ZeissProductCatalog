using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Services;
using ProductCatalog.Application.Products.Validation;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.UnitTests.Application.Products;

public sealed class ProductServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProductIdGenerator _idGenerator = Substitute.For<IProductIdGenerator>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _products,
            _categories,
            _unitOfWork,
            _idGenerator,
            new CreateProductRequestValidator(),
            new UpdateProductRequestValidator(),
            new StockRangeQueryValidator(),
            new FakeTimeProvider(Now),
            NullLogger<ProductService>.Instance);

        _categories.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private static CreateProductRequest ValidCreateRequest() => new("LNS-001", "Lens", "Desc", 10m, 5, 1);

    private static Product ExistingProduct(int id = 123456, int stock = 5)
        => Product.Create(id, "LNS-001", "Lens", null, 10m, stock, 1, Now.UtcDateTime);

    [Fact]
    public async Task CreateAsync_AssignsGeneratedIdAndPersists()
    {
        _idGenerator.NextIdAsync(Arg.Any<CancellationToken>()).Returns(654321);
        Product? added = null;
        _products.When(r => r.Add(Arg.Any<Product>())).Do(call => added = call.Arg<Product>());
        _products.GetByIdAsync(654321, Arg.Any<CancellationToken>()).Returns(_ => added);

        var result = await _sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        Assert.Equal(654321, result.Id);
        Assert.Equal(5, result.StockQuantity);
        Assert.Equal(Now.UtcDateTime, result.CreatedAtUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationAndDoesNotPersist()
    {
        var request = ValidCreateRequest() with { Name = "", Price = 0 };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateProductRequest.Name));
        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateProductRequest.Price));
        _products.DidNotReceive().Add(Arg.Any<Product>());
        await _idGenerator.DidNotReceive().NextIdAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCategory_ThrowsValidation()
    {
        _categories.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(ValidCreateRequest(), CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(CreateProductRequest.CategoryId));
        _products.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatedSku_ThrowsConflict()
    {
        _products.SkuExistsAsync("LNS-001", null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(ValidCreateRequest() with { Sku = "lns-001" }, CancellationToken.None));

        await _idGenerator.DidNotReceive().NextIdAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _products.GetByIdAsync(111111, Arg.Any<CancellationToken>()).Returns((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(111111, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingProduct()
    {
        var product = ExistingProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest("LNS-001", "Renamed", null, 15m, 9, 1), CancellationToken.None);

        Assert.Equal("Renamed", result.Name);
        Assert.Equal(9, result.StockQuantity);
        await _products.Received(1).SkuExistsAsync("LNS-001", product.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(111111, new UpdateProductRequest("LNS-001", "Lens", null, 15m, 9, 1), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct()
    {
        var product = ExistingProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _sut.DeleteAsync(product.Id, CancellationToken.None);

        _products.Received(1).Remove(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByNameAsync_WithoutTerm_ThrowsValidation(string? term)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.SearchByNameAsync(term, CancellationToken.None));
    }

    [Fact]
    public async Task SearchByNameAsync_TrimsTerm()
    {
        _products.SearchByNameAsync("lens", Arg.Any<CancellationToken>()).Returns([ExistingProduct()]);

        var result = await _sut.SearchByNameAsync("  lens ", CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByStockRangeAsync_WithInvertedRange_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.GetByStockRangeAsync(new StockRangeQuery(10, 1), CancellationToken.None));

        await _products.DidNotReceive().GetByStockRangeAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByStockRangeAsync_ReturnsProductsWithStock()
    {
        _products.GetByStockRangeAsync(0, 10, Arg.Any<CancellationToken>()).Returns([ExistingProduct(stock: 7)]);

        var result = await _sut.GetByStockRangeAsync(new StockRangeQuery(0, 10), CancellationToken.None);

        Assert.Equal(7, Assert.Single(result).StockQuantity);
    }
}
