namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>The request conflicts with the current state of a resource (e.g. duplicated SKU).</summary>
public class ConflictException(string message, Exception? innerException = null) : Exception(message, innerException);
