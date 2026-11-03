using System.Net;
using System.Net.Http.Json;
using ProductCatalog.AcceptanceTests.Support;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Testing;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.Steps;

[Binding]
public sealed class StockManagementSteps(ApiDriver driver)
{
    private ProductResponse? _product;

    [Given("a product {string} with {int} units in stock")]
    public async Task GivenAProductWithStock(string name, int stock)
    {
        _product = await driver.Api.CreateAndReadAsync(TestRequests.CreateProduct(name: name, stockQuantity: stock));
    }

    [When("I sell {int} units of it")]
    public async Task WhenISellUnits(int quantity)
    {
        driver.LastResponse = await driver.Api.DecrementStockAsync(_product!.Id, quantity);
    }

    [When("I receive a delivery of {int} units of it")]
    public async Task WhenIReceiveADelivery(int quantity)
    {
        driver.LastResponse = await driver.Api.AddToStockAsync(_product!.Id, quantity);
    }

    [Then("the request succeeds")]
    public void ThenTheRequestSucceeds()
    {
        Assert.Equal(HttpStatusCode.OK, driver.LastResponse!.StatusCode);
    }

    [Then("the request is rejected with status {int}")]
    public void ThenTheRequestIsRejected(int status)
    {
        Assert.Equal(status, (int)driver.LastResponse!.StatusCode);
    }

    [Then("the product has {int} units in stock")]
    public async Task ThenTheProductHasStock(int expected)
    {
        var current = await (await driver.Api.GetByIdAsync(_product!.Id)).Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(expected, current!.StockQuantity);
    }
}
