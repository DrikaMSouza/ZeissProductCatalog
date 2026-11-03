using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Categories.Contracts;

namespace ProductCatalog.Application.Categories.Services;

public sealed class CategoryService(ICategoryRepository categories) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
        => (await categories.GetAllAsync(cancellationToken))
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToList();
}
