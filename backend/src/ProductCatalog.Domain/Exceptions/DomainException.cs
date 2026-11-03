namespace ProductCatalog.Domain.Exceptions;

/// <summary>Base type for violations of business rules raised by the domain model.</summary>
public abstract class DomainException(string message) : Exception(message);
