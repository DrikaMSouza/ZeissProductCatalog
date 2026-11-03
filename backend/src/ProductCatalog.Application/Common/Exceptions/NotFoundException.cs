namespace ProductCatalog.Application.Common.Exceptions;

public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} with id '{key}' was not found.");
