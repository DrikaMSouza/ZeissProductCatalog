using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Testing;

/// <summary>Thread-safe stand-in for the SQL Server sequence, used with the EF Core InMemory provider.</summary>
public sealed class InMemoryProductIdGenerator : IProductIdGenerator
{
    private int _current = ProductId.MinValue - 1;

    public Task<int> NextIdAsync(CancellationToken cancellationToken)
        => Task.FromResult(Interlocked.Increment(ref _current));
}
