using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.Testing;

/// <summary>
/// Hosts the real API pipeline in memory, replacing SQL Server with an isolated EF Core InMemory database.
/// </summary>
public sealed class ProductCatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"product-catalog-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedSampleData", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProductCatalogDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ProductCatalogDbContext>>();
            services.AddDbContext<ProductCatalogDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IProductIdGenerator>();
            services.AddSingleton<IProductIdGenerator, InMemoryProductIdGenerator>();
        });
    }
}
