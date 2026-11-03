using ProductCatalog.Testing;

namespace ProductCatalog.AcceptanceTests.Support;

/// <summary>
/// One in-memory API host per scenario, injected into step classes by Reqnroll and disposed after the scenario.
/// </summary>
public sealed class ApiDriver : IDisposable
{
    private readonly ProductCatalogApiFactory _factory = new();

    public ApiDriver()
    {
        Api = new ProductsApiClient(_factory.CreateClient());
    }

    public ProductsApiClient Api { get; }

    public HttpResponseMessage? LastResponse { get; set; }

    public void Dispose() => _factory.Dispose();
}
