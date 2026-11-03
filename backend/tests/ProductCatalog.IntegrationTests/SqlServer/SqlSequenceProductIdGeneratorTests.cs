using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Domain.Products;
using ProductCatalog.Infrastructure;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.IntegrationTests.SqlServer;

/// <summary>
/// Runs against a real SQL Server (LocalDB by default) to prove that several application
/// instances allocating ids at the same time never receive duplicates.
/// Skipped when no SQL Server is reachable. Override the server with the
/// PRODUCTCATALOG_TEST_SQLSERVER environment variable (a connection string without a database).
/// </summary>
[Trait("Category", "SqlServer")]
public sealed class SqlSequenceProductIdGeneratorTests : IAsyncLifetime
{
    private const int Instances = 4;
    private const int IdsPerInstance = 50;

    private readonly string _connectionString;
    private readonly List<ServiceProvider> _instances = [];
    private bool _databaseAvailable;

    public SqlSequenceProductIdGeneratorTests()
    {
        var server = Environment.GetEnvironmentVariable("PRODUCTCATALOG_TEST_SQLSERVER")
                     ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";
        _connectionString = $"{server};Database=ProductCatalogTests_{Guid.NewGuid():N};Connect Timeout=5";
    }

    public async Task InitializeAsync()
    {
        // Each ServiceProvider plays the role of an independent API instance with its own DbContext/connection.
        for (var i = 0; i < Instances; i++)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = _connectionString,
                })
                .Build();

            _instances.Add(new ServiceCollection()
                .AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
                .AddInfrastructure(configuration)
                .BuildServiceProvider());
        }

        try
        {
            await using var scope = _instances[0].CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>().Database.MigrateAsync();
            _databaseAvailable = true;
        }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException or PlatformNotSupportedException)
        {
            _databaseAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_databaseAvailable)
        {
            await using var scope = _instances[0].CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>().Database.EnsureDeletedAsync();
        }

        foreach (var instance in _instances)
        {
            await instance.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task ConcurrentInstances_NeverReceiveDuplicateIds()
    {
        Skip.IfNot(_databaseAvailable, "SQL Server is not available.");

        var allocations = _instances.Select(async instance =>
        {
            var ids = new List<int>();
            for (var i = 0; i < IdsPerInstance; i++)
            {
                await using var scope = instance.CreateAsyncScope();
                ids.Add(await scope.ServiceProvider.GetRequiredService<IProductIdGenerator>().NextIdAsync(CancellationToken.None));
            }

            return ids;
        });

        var ids = (await Task.WhenAll(allocations)).SelectMany(x => x).ToList();

        Assert.Equal(Instances * IdsPerInstance, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, ProductId.MinValue, ProductId.MaxValue));
    }
}
