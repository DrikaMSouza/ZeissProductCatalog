using ProductCatalog.Domain.Exceptions;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Domain.Categories;

public sealed class Category
{
    public const int NameMaxLength = 100;

    private readonly List<Product> _products = [];

    // Required by EF Core.
    private Category()
    {
        Name = string.Empty;
    }

    public Category(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Category name is required.");
        }

        Id = id;
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();
}
