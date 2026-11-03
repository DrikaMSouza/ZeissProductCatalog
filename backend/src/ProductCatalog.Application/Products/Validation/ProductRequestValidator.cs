using FluentValidation;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Validation;

/// <summary>Rules shared by create and update requests.</summary>
public abstract class ProductRequestValidator<T> : AbstractValidator<T>
    where T : IProductRequest
{
    public const decimal MaxPrice = 1_000_000m;

    protected ProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(Product.SkuMaxLength)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU may only contain letters, digits and dashes.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Product.DescriptionMaxLength);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxPrice)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);
    }
}
