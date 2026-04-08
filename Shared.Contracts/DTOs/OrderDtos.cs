namespace Shared.Contracts.DTOs;

public record ProductDto(
    long ProductId,
    string Name,
    string Description,
    decimal Price,
    string Category,
    int StockQuantity
);

public record CustomerDto(
    string CustomerId,
    string Name,
    string Email
);

public record OrderItemDto(
    long ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public record OrderDto(
    Guid OrderId,
    string CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemDto> Items,
    string? ShipmentReference,
    string? TrackingInfo,
    string? FailureReason,
    InventoryStatusDto? Inventory,
    PaymentStatusDto? Payment,
    ShipmentStatusDto? Shipment,
    Guid CorrelationId
);

public record InventoryStatusDto(
    bool Success,
    string Outcome,
    string? FailureReason,
    DateTime ProcessedAt
);

public record PaymentStatusDto(
    bool Approved,
    string Outcome,
    string? TransactionId,
    string? RejectionReason,
    decimal Amount,
    DateTime ProcessedAt
);

public record ShipmentStatusDto(
    bool Success,
    string Outcome,
    string? ShipmentReference,
    DateTime? EstimatedDispatchDate,
    string? FailureReason,
    DateTime CreatedAt
);

public record OrderStatusDto(
    Guid OrderId,
    string Status,
    DateTime LastUpdated,
    string? ShipmentReference,
    string? FailureReason,
    List<OrderStatusHistoryDto> History
);

public record OrderStatusHistoryDto(
    string Status,
    DateTime Timestamp,
    string? Notes
);

public record DashboardSummaryDto(
    int TotalOrders,
    int PendingOrders,
    int CompletedOrders,
    int FailedOrders,
    decimal TotalRevenue,
    List<OrdersByStatusDto> OrdersByStatus
);

public record OrdersByStatusDto(
    string Status,
    int Count
);

public record CheckoutRequestDto(
    string CustomerId,
    string CustomerName,
    string ShippingAddress,
    List<CheckoutItemDto> Items
);

public record CheckoutItemDto(
    long ProductId,
    int Quantity
);

public record CheckoutResponseDto(
    Guid OrderId,
    string Status,
    string Message
);
