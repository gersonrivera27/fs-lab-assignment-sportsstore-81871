using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Shared.Contracts.Events;
using Serilog;

namespace Inventory.Service;

public class InventoryWorker : BackgroundService
{
    private readonly ILogger<InventoryWorker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    // Simulated stock levels
    private static readonly Dictionary<long, int> Stock = new()
    {
        { 1, 50 }, { 2, 200 }, { 3, 150 }, { 4, 75 }, { 5, 5 },
        { 6, 300 }, { 7, 50 }, { 8, 30 }, { 9, 10 }
    };

    public InventoryWorker(ILogger<InventoryWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel == null) return;

        const string exchange = "inventory.check";
        const string resultExchange = "inventory.completed";
        const string queueName = "inventory-service.check";

        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(resultExchange, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(queueName, exchange, "", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<InventoryCheckRequested>(json);
                if (request == null) return;

                _logger.LogInformation(
                    "InventoryCheckRequested received for Order {OrderId}, CorrelationId={CorrelationId}, Items={ItemCount}",
                    request.OrderId, request.CorrelationId, request.Items.Count);

                // Simulate a small processing delay
                await Task.Delay(1500, stoppingToken);

                // Check stock availability
                string? failureReason = null;
                foreach (var item in request.Items)
                {
                    var available = Stock.GetValueOrDefault(item.ProductId, 0);
                    if (available < item.Quantity)
                    {
                        failureReason = $"Insufficient stock for product {item.ProductName}: requested {item.Quantity}, available {available}";
                        break;
                    }
                }

                var success = failureReason == null;

                if (success)
                {
                    // Reserve stock (simulate)
                    foreach (var item in request.Items)
                        Stock[item.ProductId] -= item.Quantity;

                    _logger.LogInformation("Inventory CONFIRMED for Order {OrderId}", request.OrderId);
                }
                else
                {
                    _logger.LogWarning("Inventory FAILED for Order {OrderId}: {Reason}", request.OrderId, failureReason);
                }

                var result = new InventoryCheckCompleted(
                    request.OrderId, request.CorrelationId, success, failureReason, DateTime.UtcNow);

                var resultJson = JsonSerializer.Serialize(result);
                var body = Encoding.UTF8.GetBytes(resultJson);
                var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
                await _channel.BasicPublishAsync(resultExchange, "", false, props, body);

                _logger.LogInformation("Published InventoryCheckCompleted for Order {OrderId}, Success={Success}",
                    request.OrderId, success);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inventory check");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Inventory.Service listening for messages...");
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
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                    UserName = _config["RabbitMQ:Username"] ?? "guest",
                    Password = _config["RabbitMQ:Password"] ?? "guest"
                };
                _connection = await factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                _logger.LogInformation("Inventory.Service connected to RabbitMQ");
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

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}
