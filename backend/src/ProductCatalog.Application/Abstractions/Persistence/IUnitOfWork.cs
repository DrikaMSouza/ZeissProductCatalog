namespace ProductCatalog.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <summary>Persists pending changes.</summary>
    /// <exception cref="Common.Exceptions.ConcurrencyConflictException">The entity was changed by another request.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
