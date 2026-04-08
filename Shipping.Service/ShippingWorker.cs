using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Shared.Contracts.Events;

namespace Shipping.Service;

public class ShippingWorker : BackgroundService
{
    private readonly ILogger<ShippingWorker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    public ShippingWorker(ILogger<ShippingWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel == null) return;

        const string exchange = "shipping.requested";
        const string resultExchange = "shipping.created";
        const string queueName = "shipping-service.requested";

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
                var request = JsonSerializer.Deserialize<ShippingRequested>(json);
                if (request == null) return;

                _logger.LogInformation(
                    "ShippingRequested for Order {OrderId}, Customer={CustomerName}, Address={Address}, CorrelationId={CorrelationId}",
                    request.OrderId, request.CustomerName, request.ShippingAddress, request.CorrelationId);

                // Simulate shipping creation delay
                await Task.Delay(1800, stoppingToken);

                var shipmentRef = $"SS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                var estimatedDispatch = DateTime.UtcNow.AddDays(3);

                _logger.LogInformation(
                    "Shipment CREATED for Order {OrderId}: Reference={Reference}, EstimatedDispatch={EstimatedDispatch}",
                    request.OrderId, shipmentRef, estimatedDispatch);

                var result = new ShippingCreated(
                    request.OrderId, request.CorrelationId, true,
                    shipmentRef, estimatedDispatch, null, DateTime.UtcNow);

                var resultJson = JsonSerializer.Serialize(result);
                var body = Encoding.UTF8.GetBytes(resultJson);
                var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
                await _channel.BasicPublishAsync(resultExchange, "", false, props, body);

                _logger.LogInformation("Published ShippingCreated for Order {OrderId}", request.OrderId);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipment");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Shipping.Service listening for messages...");
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
                _logger.LogInformation("Shipping.Service connected to RabbitMQ");
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
