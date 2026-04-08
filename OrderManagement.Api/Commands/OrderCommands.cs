using MediatR;
using OrderManagement.Api.Data;
using OrderManagement.Api.Data.Entities;
using OrderManagement.Api.Messaging;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;
using Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api.Commands;

// ─── Checkout Order ───────────────────────────────────────────────────────────

public record CheckoutOrderCommand(CheckoutRequestDto Request) : IRequest<CheckoutResponseDto>;

public class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, CheckoutResponseDto>
{
    private readonly OrderDbContext _db;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CheckoutOrderCommandHandler> _logger;

    public CheckoutOrderCommandHandler(
        OrderDbContext db,
        IMessagePublisher publisher,
        ILogger<CheckoutOrderCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CheckoutResponseDto> Handle(CheckoutOrderCommand request, CancellationToken ct)
    {
        var req = request.Request;

        // Resolve products and calculate totals
        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, ct);

        var orderItems = req.Items.Select(i =>
        {
            var product = products[i.ProductId];
            return new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = product.Name,
                Quantity = i.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        var total = orderItems.Sum(i => i.Quantity * i.UnitPrice);
        var correlationId = Guid.NewGuid();

        var order = new Order
        {
            CustomerId = req.CustomerId,
            CustomerName = req.CustomerName,
            ShippingAddress = req.ShippingAddress,
            TotalAmount = total,
            Status = OrderStatus.Submitted,
            CorrelationId = correlationId,
            Items = orderItems
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Submitted.ToString(),
            Notes = "Order submitted by customer"
        });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} submitted by {CustomerName} (CustomerId={CustomerId}). Total={Total}, Items={ItemCount}, CorrelationId={CorrelationId}, EventType={EventType}",
            order.OrderId, req.CustomerName, req.CustomerId, total, orderItems.Count, correlationId, nameof(OrderSubmittedEvent));

        // Publish OrderSubmitted event → triggers Inventory service
        var eventItems = orderItems.Select(i => new OrderItemContract(
            i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();

        await _publisher.PublishAsync("order.submitted", "", new OrderSubmittedEvent(
            order.OrderId, correlationId, req.CustomerId, req.CustomerName,
            eventItems, total, DateTime.UtcNow));

        // Transition to InventoryPending
        order.Status = OrderStatus.InventoryPending;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            Status = OrderStatus.InventoryPending.ToString(),
            Notes = "Inventory check requested"
        });
        await _db.SaveChangesAsync(ct);

        // Publish InventoryCheckRequested
        await _publisher.PublishAsync("inventory.check", "", new InventoryCheckRequested(
            order.OrderId, correlationId, eventItems, DateTime.UtcNow));

        _logger.LogInformation(
            "Published inventory request for Order {OrderId}. CustomerId={CustomerId}, CorrelationId={CorrelationId}, EventType={EventType}",
            order.OrderId, order.CustomerId, correlationId, nameof(InventoryCheckRequested));

        return new CheckoutResponseDto(order.OrderId, order.Status.ToString(), "Order submitted successfully");
    }
}

// ─── Cancel Order ─────────────────────────────────────────────────────────────

public record CancelOrderCommand(Guid OrderId) : IRequest<bool>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(OrderDbContext db, ILogger<CancelOrderCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync([request.OrderId], ct);
        if (order == null) return false;

        var cancellableStatuses = new[] { OrderStatus.Submitted, OrderStatus.InventoryPending };
        if (!cancellableStatuses.Contains(order.Status)) return false;

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            Status = OrderStatus.Cancelled.ToString(),
            Notes = "Cancelled by customer"
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Order {OrderId} cancelled by customer. CustomerId={CustomerId}, CorrelationId={CorrelationId}, EventType={EventType}",
            request.OrderId, order.CustomerId, order.CorrelationId, nameof(CancelOrderCommand));
        return true;
    }
}
