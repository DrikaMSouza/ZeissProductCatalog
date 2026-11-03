namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>All available 6-digit product identifiers have been allocated.</summary>
public sealed class ProductIdExhaustedException(Exception innerException)
    : Exception("No more 6-digit product identifiers are available.", innerException);
