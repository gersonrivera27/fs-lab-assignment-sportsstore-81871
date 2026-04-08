using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Serilog;

namespace OrderManagement.Api.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message);
}

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare all exchanges used in the platform
        var exchanges = new[]
        {
            "order.submitted", "inventory.check", "inventory.completed",
            "payment.requested", "payment.processed",
            "shipping.requested", "shipping.created",
            "order.completed", "order.failed"
        };

        foreach (var exchange in exchanges)
        {
            _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
        }
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(exchange, routingKey, false, props, body);

        var orderId = TryGetProperty(message, "OrderId");
        var customerId = TryGetProperty(message, "CustomerId");
        var correlationId = TryGetProperty(message, "CorrelationId");

        _logger.LogInformation(
            "Published message to exchange {Exchange} with routing key {RoutingKey}. MessageType: {MessageType}, EventType: {EventType}, OrderId: {OrderId}, CustomerId: {CustomerId}, CorrelationId: {CorrelationId}",
            exchange, routingKey, typeof(T).Name, typeof(T).Name, orderId, customerId, correlationId);
    }

    private static object? TryGetProperty<T>(T message, string propertyName) =>
        typeof(T).GetProperty(propertyName)?.GetValue(message);

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}
