using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;
using ProductCatalog.Testing;

namespace ProductCatalog.IntegrationTests.Products;

/// <summary>Exercises the full HTTP pipeline (routing, model binding, validation, error mapping, EF Core).</summary>
public sealed class ProductsEndpointsTests : IClassFixture<ProductCatalogApiFactory>
{
    private readonly ProductsApiClient _api;

    public ProductsEndpointsTests(ProductCatalogApiFactory factory)
    {
        _api = new ProductsApiClient(factory.CreateClient());
    }

    [Fact]
    public async Task Post_CreatesProductWithSixDigitId_AndReturnsLocation()
    {
        var response = await _api.CreateAsync(TestRequests.CreateProduct(stockQuantity: 7));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.InRange(product!.Id, ProductId.MinValue, ProductId.MaxValue);
        Assert.Equal(7, product.StockQuantity);
        Assert.Equal("Spectacle Lenses", product.CategoryName);
        Assert.EndsWith($"/api/products/{product.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_WithInvalidPayload_ReturnsValidationProblem()
    {
        var response = await _api.CreateAsync(new CreateProductRequest("", "", null, -5m, -1, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("sku", problem!.Errors.Keys);
        Assert.Contains("name", problem.Errors.Keys);
        Assert.Contains("price", problem.Errors.Keys);
        Assert.Contains("stockQuantity", problem.Errors.Keys);
        Assert.Contains("categoryId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Post_WithUnknownCategory_ReturnsValidationProblem()
    {
        var response = await _api.CreateAsync(TestRequests.CreateProduct(categoryId: 9999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithDuplicatedSku_ReturnsConflict()
    {
        var first = await _api.CreateAndReadAsync(TestRequests.CreateProduct());

        var response = await _api.CreateAsync(TestRequests.CreateProduct(sku: first.Sku.ToLowerInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ManyProductsInParallel_GeneratesUniqueIds()
    {
        var created = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => _api.CreateAndReadAsync(TestRequests.CreateProduct())));

        Assert.Equal(created.Length, created.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task GetAll_IncludesStockOfEveryProduct()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 13));

        var products = await (await _api.GetAllAsync()).Content.ReadFromJsonAsync<List<ProductResponse>>();

        Assert.Equal(13, Assert.Single(products!, p => p.Id == created.Id).StockQuantity);
    }

    [Fact]
    public async Task GetById_ReturnsProduct_Or404()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct());

        var found = await _api.GetByIdAsync(created.Id);
        var missing = await _api.GetByIdAsync(999_998);

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(created, await found.Content.ReadFromJsonAsync<ProductResponse>());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Put_UpdatesProduct()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct());

        var response = await _api.UpdateAsync(created.Id, TestRequests.UpdateProduct(created.Sku, name: "Renamed", stockQuantity: 42, categoryId: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(42, updated.StockQuantity);
        Assert.Equal("Frames", updated.CategoryName);
    }

    [Fact]
    public async Task Put_OnUnknownProduct_Returns404()
    {
        var response = await _api.UpdateAsync(999_997, TestRequests.UpdateProduct("ANY-SKU"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesProduct()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct());

        var response = await _api.DeleteAsync(created.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.GetByIdAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.DeleteAsync(created.Id)).StatusCode);
    }

    [Fact]
    public async Task DecrementStock_ReturnsOkWithUpdatedStock()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 10));

        var response = await _api.DecrementStockAsync(created.Id, 4);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(6, (await response.Content.ReadFromJsonAsync<ProductResponse>())!.StockQuantity);
    }

    [Fact]
    public async Task DecrementStock_BeyondAvailable_ReturnsConflictAndKeepsStock()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 2));

        var response = await _api.DecrementStockAsync(created.Id, 3);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var product = await (await _api.GetByIdAsync(created.Id)).Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(2, product!.StockQuantity);
    }

    [Fact]
    public async Task AddToStock_ReturnsOkWithUpdatedStock()
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 1));

        var response = await _api.AddToStockAsync(created.Id, 9);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10, (await response.Content.ReadFromJsonAsync<ProductResponse>())!.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task StockMovements_WithNonPositiveQuantity_ReturnBadRequest(int quantity)
    {
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct());

        Assert.Equal(HttpStatusCode.BadRequest, (await _api.AddToStockAsync(created.Id, quantity)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _api.DecrementStockAsync(created.Id, quantity)).StatusCode);
    }

    [Fact]
    public async Task StockMovements_OnUnknownProduct_Return404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _api.AddToStockAsync(999_996, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.DecrementStockAsync(999_996, 1)).StatusCode);
    }

    [Fact]
    public async Task Search_MatchesPartialNameCaseInsensitively()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var created = await _api.CreateAndReadAsync(TestRequests.CreateProduct(name: $"Ultra {marker} Lens"));

        var response = await _api.SearchAsync(marker.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Equal(created.Id, Assert.Single(products!).Id);
    }

    [Fact]
    public async Task Search_WithoutTerm_ReturnsBadRequest()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _api.SearchAsync(" ")).StatusCode);
    }

    [Fact]
    public async Task StockLevel_ReturnsOnlyProductsWithinInclusiveRange()
    {
        var low = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 777_001));
        var high = await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 777_003));
        await _api.CreateAndReadAsync(TestRequests.CreateProduct(stockQuantity: 777_004));

        var response = await _api.GetByStockLevelAsync(777_001, 777_003);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Equal([low.Id, high.Id], products!.Select(p => p.Id));
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(-1, 5)]
    [InlineData(null, null)]
    public async Task StockLevel_WithInvalidRange_ReturnsBadRequest(int? min, int? max)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _api.GetByStockLevelAsync(min, max)).StatusCode);
    }

    [Fact]
    public async Task Categories_ReturnsSeededReferenceData()
    {
        var categories = await _api.GetCategoriesAndReadAsync();

        Assert.Equal(5, categories.Count);
    }
}
