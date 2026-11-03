namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>The resource was modified by another request after it was read. The client may retry.</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : ConflictException("The resource was modified by another request. Please reload it and try again.", innerException);
