namespace Shared.Contracts.Events;

public record OrderSubmittedEvent(
    Guid OrderId,
    Guid CorrelationId,
    string CustomerId,
    string CustomerName,
    List<OrderItemContract> Items,
    decimal TotalAmount,
    DateTime SubmittedAt
);

public record OrderItemContract(
    long ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record InventoryCheckRequested(
    Guid OrderId,
    Guid CorrelationId,
    List<OrderItemContract> Items,
    DateTime RequestedAt
);

public record InventoryCheckCompleted(
    Guid OrderId,
    Guid CorrelationId,
    bool Success,
    string? FailureReason,
    DateTime CompletedAt
);

public record PaymentProcessingRequested(
    Guid OrderId,
    Guid CorrelationId,
    string CustomerId,
    decimal Amount,
    DateTime RequestedAt
);

public record PaymentProcessed(
    Guid OrderId,
    Guid CorrelationId,
    bool Approved,
    string? TransactionId,
    string? RejectionReason,
    DateTime ProcessedAt
);

public record ShippingRequested(
    Guid OrderId,
    Guid CorrelationId,
    string CustomerName,
    string ShippingAddress,
    List<OrderItemContract> Items,
    DateTime RequestedAt
);

public record ShippingCreated(
    Guid OrderId,
    Guid CorrelationId,
    bool Success,
    string? ShipmentReference,
    DateTime? EstimatedDispatchDate,
    string? FailureReason,
    DateTime CreatedAt
);

public record OrderCompleted(
    Guid OrderId,
    Guid CorrelationId,
    string ShipmentReference,
    DateTime CompletedAt
);

public record OrderFailed(
    Guid OrderId,
    Guid CorrelationId,
    string Reason,
    string Stage,
    DateTime FailedAt
);
