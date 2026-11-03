using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products.Services;
using ProductCatalog.Domain.Exceptions;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.UnitTests.Application.Products;

public sealed class ProductStockServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly ProductStockService _sut;

    public ProductStockServiceTests()
    {
        _sut = new ProductStockService(_products, _unitOfWork, _timeProvider, NullLogger<ProductStockService>.Instance);
    }

    private Product GivenProduct(int stock)
    {
        var product = Product.Create(123456, "LNS-001", "Lens", null, 10m, stock, 1, Now.UtcDateTime);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    [Fact]
    public async Task DecrementStockAsync_ReturnsUpdatedProduct()
    {
        var product = GivenProduct(stock: 10);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        var result = await _sut.DecrementStockAsync(product.Id, 3, CancellationToken.None);

        Assert.Equal(7, result.StockQuantity);
        Assert.Equal(Now.AddMinutes(1).UtcDateTime, result.UpdatedAtUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecrementStockAsync_WithInsufficientStock_ThrowsAndDoesNotSave()
    {
        var product = GivenProduct(stock: 1);

        await Assert.ThrowsAsync<InsufficientStockException>(() => _sut.DecrementStockAsync(product.Id, 2, CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToStockAsync_ReturnsUpdatedProduct()
    {
        var product = GivenProduct(stock: 1);

        var result = await _sut.AddToStockAsync(product.Id, 4, CancellationToken.None);

        Assert.Equal(5, result.StockQuantity);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockMovement_OnUnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddToStockAsync(999999, 1, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DecrementStockAsync(999999, 1, CancellationToken.None));
    }

    [Fact]
    public async Task StockMovement_WhenAnotherInstanceChangedTheProduct_PropagatesConcurrencyConflict()
    {
        var product = GivenProduct(stock: 10);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException("row version mismatch")));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => _sut.DecrementStockAsync(product.Id, 1, CancellationToken.None));
    }
}
