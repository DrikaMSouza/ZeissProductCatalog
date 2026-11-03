using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Domain.Categories;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(ProductCatalogDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
        => await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(c => c.Id == id, cancellationToken);
}
