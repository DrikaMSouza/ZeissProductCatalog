namespace ProductCatalog.Application.Abstractions.Services;

/// <summary>
/// Generates unique 6-digit product identifiers. Implementations must be safe to use
/// from several application instances running at the same time.
/// </summary>
public interface IProductIdGenerator
{
    Task<int> NextIdAsync(CancellationToken cancellationToken);
}
