namespace ProductCatalog.Domain.Exceptions;

public sealed class InsufficientStockException(int productId, int availableStock, int requestedQuantity)
    : DomainException($"Cannot remove {requestedQuantity} unit(s) from product {productId}: only {availableStock} unit(s) available.")
{
    public int ProductId { get; } = productId;

    public int AvailableStock { get; } = availableStock;

    public int RequestedQuantity { get; } = requestedQuantity;
}
