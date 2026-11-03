namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Inclusive stock range. Either bound may be omitted, but not both.</summary>
public sealed record StockRangeQuery(int? Min, int? Max);
