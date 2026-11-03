using System.Net;
using System.Net.Http.Json;
using ProductCatalog.AcceptanceTests.Support;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Testing;
using Reqnroll;

namespace ProductCatalog.AcceptanceTests.Steps;

[Binding]
public sealed class ProductDiscoverySteps(ApiDriver driver)
{
    private List<ProductResponse> _listed = [];

    [Given("the following products exist")]
    public async Task GivenTheFollowingProductsExist(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            await driver.Api.CreateAndReadAsync(TestRequests.CreateProduct(name: row["Name"], stockQuantity: int.Parse(row["Stock"])));
        }
    }

    [When("I search for products named {string}")]
    public async Task WhenISearchForProductsNamed(string term)
    {
        await ReadListAsync(driver.Api.SearchAsync(term));
    }

    [When("I list the products with stock between {int} and {int}")]
    public async Task WhenIListTheProductsWithStockBetween(int min, int max)
    {
        await ReadListAsync(driver.Api.GetByStockLevelAsync(min, max));
    }

    [Then("I see exactly these products")]
    public void ThenISeeExactlyTheseProducts(DataTable table)
    {
        var expected = table.Rows.Select(r => r["Name"]).Order();
        Assert.Equal(expected, _listed.Select(p => p.Name).Order());
    }

    [Then("every listed product shows its stock")]
    public void ThenEveryListedProductShowsItsStock()
    {
        Assert.All(_listed, p => Assert.InRange(p.StockQuantity, 0, 5));
    }

    private async Task ReadListAsync(Task<HttpResponseMessage> request)
    {
        driver.LastResponse = await request;
        Assert.Equal(HttpStatusCode.OK, driver.LastResponse.StatusCode);
        _listed = (await driver.LastResponse.Content.ReadFromJsonAsync<List<ProductResponse>>())!;
    }
}
