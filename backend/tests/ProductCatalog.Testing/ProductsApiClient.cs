using System.Net.Http.Json;
using ProductCatalog.Application.Categories.Contracts;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Testing;

/// <summary>Thin typed wrapper around the HTTP endpoints, so tests read as API usage rather than URL plumbing.</summary>
public sealed class ProductsApiClient(HttpClient httpClient)
{
    private const string Products = "api/products";

    public Task<HttpResponseMessage> GetAllAsync() => httpClient.GetAsync(Products);

    public Task<HttpResponseMessage> GetByIdAsync(int id) => httpClient.GetAsync($"{Products}/{id}");

    public Task<HttpResponseMessage> SearchAsync(string? name)
        => httpClient.GetAsync($"{Products}/search?name={Uri.EscapeDataString(name ?? string.Empty)}");

    public Task<HttpResponseMessage> GetByStockLevelAsync(int? min, int? max)
    {
        var query = new List<string>();
        if (min.HasValue)
        {
            query.Add($"min={min}");
        }

        if (max.HasValue)
        {
            query.Add($"max={max}");
        }

        return httpClient.GetAsync($"{Products}/stock-level?{string.Join('&', query)}");
    }

    public Task<HttpResponseMessage> CreateAsync(CreateProductRequest request) => httpClient.PostAsJsonAsync(Products, request);

    public Task<HttpResponseMessage> UpdateAsync(int id, UpdateProductRequest request) => httpClient.PutAsJsonAsync($"{Products}/{id}", request);

    public Task<HttpResponseMessage> DeleteAsync(int id) => httpClient.DeleteAsync($"{Products}/{id}");

    public Task<HttpResponseMessage> DecrementStockAsync(int id, int quantity)
        => httpClient.PostAsync($"{Products}/{id}/decrement-stock/{quantity}", content: null);

    public Task<HttpResponseMessage> AddToStockAsync(int id, int quantity)
        => httpClient.PostAsync($"{Products}/{id}/add-to-stock/{quantity}", content: null);

    public Task<HttpResponseMessage> GetCategoriesAsync() => httpClient.GetAsync("api/categories");

    public async Task<ProductResponse> CreateAndReadAsync(CreateProductRequest request)
    {
        var response = await CreateAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAndReadAsync()
        => (await httpClient.GetFromJsonAsync<List<CategoryResponse>>("api/categories"))!;
}
