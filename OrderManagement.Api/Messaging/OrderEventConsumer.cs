using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Shared.Contracts.Events;
using Shared.Contracts.Enums;
using OrderManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api.Messaging;

public class OrderEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderEventConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel == null) return;

        // Listen for inventory results
        await ConsumeQueue<InventoryCheckCompleted>(
            _channel, "order-api.inventory.completed", "inventory.completed",
            HandleInventoryCompletedAsync, stoppingToken);

        // Listen for payment results
        await ConsumeQueue<PaymentProcessed>(
            _channel, "order-api.payment.processed", "payment.processed",
            HandlePaymentProcessedAsync, stoppingToken);

        // Listen for shipping results
        await ConsumeQueue<ShippingCreated>(
            _channel, "order-api.shipping.created", "shipping.created",
            HandleShippingCreatedAsync, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken ct)
    {
        var retries = 0;
        while (retries < 10 && !ct.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };
                _connection = await factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                _logger.LogInformation("OrderEventConsumer connected to RabbitMQ");
                return;
            }
            catch (Exception ex)
            {
                retries++;
                _logger.LogWarning(ex, "RabbitMQ not ready (attempt {Retry}/10). Retrying in 5s...", retries);
                await Task.Delay(5000, ct);
            }
        }
    }

    private async Task ConsumeQueue<T>(IChannel channel, string queueName, string exchange,
        Func<T, Task> handler, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(queueName, exchange, "", cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json);
                if (message != null) await handler(message);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {MessageType} message", typeof(T).Name);
                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };
        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
    }

    private async Task HandleInventoryCompletedAsync(InventoryCheckCompleted msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order == null) return;

        _logger.LogInformation(
            "Inventory result for Order {OrderId}: Success={Success}, CorrelationId={CorrelationId}, EventType={EventType}, CustomerId={CustomerId}",
            msg.OrderId, msg.Success, msg.CorrelationId, nameof(InventoryCheckCompleted), order.CustomerId);

        db.InventoryRecords.Add(new Data.Entities.InventoryRecord
        {
            OrderId = msg.OrderId,
            Success = msg.Success,
            FailureReason = msg.FailureReason,
            ProcessedAt = msg.CompletedAt
        });

        if (msg.Success)
        {
            order.Status = OrderStatus.InventoryConfirmed;
            AddHistory(order, OrderStatus.InventoryConfirmed, "Inventory check passed");

            // Trigger payment processing
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync("payment.requested", "", new PaymentProcessingRequested(
                order.OrderId, order.CorrelationId, order.CustomerId, order.TotalAmount, DateTime.UtcNow));
            order.Status = OrderStatus.PaymentPending;
            AddHistory(order, OrderStatus.PaymentPending, "Payment processing requested");
        }
        else
        {
            order.Status = OrderStatus.InventoryFailed;
            order.FailureReason = msg.FailureReason;
            AddHistory(order, OrderStatus.InventoryFailed, msg.FailureReason);

            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync("order.failed", "", new OrderFailed(
                order.OrderId, order.CorrelationId, msg.FailureReason ?? "Inventory failed", "Inventory", DateTime.UtcNow));
            order.Status = OrderStatus.Failed;
            AddHistory(order, OrderStatus.Failed, "Order failed due to inventory");
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task HandlePaymentProcessedAsync(PaymentProcessed msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == msg.OrderId);
        if (order == null) return;

        _logger.LogInformation(
            "Payment result for Order {OrderId}: Approved={Approved}, TransactionId={TransactionId}, CorrelationId={CorrelationId}, EventType={EventType}, CustomerId={CustomerId}",
            msg.OrderId, msg.Approved, msg.TransactionId, msg.CorrelationId, nameof(PaymentProcessed), order.CustomerId);

        db.PaymentRecords.Add(new Data.Entities.PaymentRecord
        {
            OrderId = msg.OrderId,
            Approved = msg.Approved,
            TransactionId = msg.TransactionId,
            RejectionReason = msg.RejectionReason,
            Amount = order.TotalAmount,
            ProcessedAt = msg.ProcessedAt
        });

        if (msg.Approved)
        {
            order.Status = OrderStatus.PaymentApproved;
            AddHistory(order, OrderStatus.PaymentApproved, $"Payment approved. Transaction: {msg.TransactionId}");

            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var shippingItems = order.Items.Select(i => new Shared.Contracts.Events.OrderItemContract(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();
            await publisher.PublishAsync("shipping.requested", "", new ShippingRequested(
                order.OrderId, order.CorrelationId,
                order.CustomerName, order.ShippingAddress, shippingItems, DateTime.UtcNow));
            order.Status = OrderStatus.ShippingPending;
            AddHistory(order, OrderStatus.ShippingPending, "Shipping requested");
        }
        else
        {
            order.Status = OrderStatus.PaymentFailed;
            order.FailureReason = msg.RejectionReason;
            AddHistory(order, OrderStatus.PaymentFailed, msg.RejectionReason);

            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync("order.failed", "", new OrderFailed(
                order.OrderId, order.CorrelationId, msg.RejectionReason ?? "Payment rejected", "Payment", DateTime.UtcNow));
            order.Status = OrderStatus.Failed;
            AddHistory(order, OrderStatus.Failed, "Order failed due to payment rejection");
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task HandleShippingCreatedAsync(ShippingCreated msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order == null) return;

        _logger.LogInformation(
            "Shipping result for Order {OrderId}: Success={Success}, Reference={Reference}, CorrelationId={CorrelationId}, EventType={EventType}, CustomerId={CustomerId}",
            msg.OrderId, msg.Success, msg.ShipmentReference, msg.CorrelationId, nameof(ShippingCreated), order.CustomerId);

        db.ShipmentRecords.Add(new Data.Entities.ShipmentRecord
        {
            OrderId = msg.OrderId,
            Success = msg.Success,
            ShipmentReference = msg.ShipmentReference,
            EstimatedDispatchDate = msg.EstimatedDispatchDate,
            FailureReason = msg.FailureReason,
            CreatedAt = msg.CreatedAt
        });

        if (msg.Success)
        {
            order.Status = OrderStatus.ShippingCreated;
            AddHistory(order, OrderStatus.ShippingCreated, $"Shipment {msg.ShipmentReference} created");

            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync("order.completed", "", new OrderCompleted(
                order.OrderId, order.CorrelationId, msg.ShipmentReference!, DateTime.UtcNow));
            order.Status = OrderStatus.Completed;
            AddHistory(order, OrderStatus.Completed, "Order completed successfully");
        }
        else
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = msg.FailureReason;
            AddHistory(order, OrderStatus.Failed, $"Shipping failed: {msg.FailureReason}");
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static void AddHistory(Data.Entities.Order order, OrderStatus status, string? notes = null)
    {
        order.StatusHistory.Add(new Data.Entities.OrderStatusHistory
        {
            OrderId = order.OrderId,
            Status = status.ToString(),
            Timestamp = DateTime.UtcNow,
            Notes = notes
        });
    }

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}
