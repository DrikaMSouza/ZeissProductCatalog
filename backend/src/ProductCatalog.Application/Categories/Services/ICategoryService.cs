using ProductCatalog.Application.Categories.Contracts;

namespace ProductCatalog.Application.Categories.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken);
}
