using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Application.Categories.Services;
using ProductCatalog.Application.Products.Services;

namespace ProductCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductStockService, ProductStockService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
