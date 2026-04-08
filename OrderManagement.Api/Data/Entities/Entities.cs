using System.ComponentModel.DataAnnotations;
using Shared.Contracts.Enums;

namespace OrderManagement.Api.Data.Entities;

public class Order
{
    [Key]
    public Guid OrderId { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string? FailureReason { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public InventoryRecord? InventoryRecord { get; set; }
    public PaymentRecord? PaymentRecord { get; set; }
    public ShipmentRecord? ShipmentRecord { get; set; }
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}

public class OrderItem
{
    [Key]
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Order Order { get; set; } = null!;
}

public class Product
{
    [Key]
    public long ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 100;
}

public class InventoryRecord
{
    [Key]
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Order Order { get; set; } = null!;
}

public class PaymentRecord
{
    [Key]
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public bool Approved { get; set; }
    public string? TransactionId { get; set; }
    public string? RejectionReason { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Order Order { get; set; } = null!;
}

public class ShipmentRecord
{
    [Key]
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public string? ShipmentReference { get; set; }
    public DateTime? EstimatedDispatchDate { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
}

public class OrderStatusHistory
{
    [Key]
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Order Order { get; set; } = null!;
}
