using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Abstractions.Persistence;
using ProductCatalog.Application.Common.Exceptions;

namespace ProductCatalog.Infrastructure.Persistence;

/// <summary>Commits the DbContext and translates EF Core failures into application exceptions.</summary>
internal sealed class UnitOfWork(ProductCatalogDbContext dbContext) : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            // Lost a race against another request that inserted the same unique value (e.g. SKU).
            throw new ConflictException("A product with the same unique values already exists.", ex);
        }
    }
}
