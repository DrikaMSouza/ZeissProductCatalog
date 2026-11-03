using FluentValidation;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validation;

public sealed class StockRangeQueryValidator : AbstractValidator<StockRangeQuery>
{
    public StockRangeQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Min.HasValue || x.Max.HasValue)
            .WithName("range")
            .WithMessage("At least one of 'min' or 'max' must be provided.");

        RuleFor(x => x.Min).GreaterThanOrEqualTo(0).When(x => x.Min.HasValue);

        RuleFor(x => x.Max).GreaterThanOrEqualTo(0).When(x => x.Max.HasValue);

        RuleFor(x => x.Max)
            .GreaterThanOrEqualTo(x => x.Min!.Value)
            .When(x => x.Min.HasValue && x.Max.HasValue)
            .WithMessage("'max' must be greater than or equal to 'min'.");
    }
}
