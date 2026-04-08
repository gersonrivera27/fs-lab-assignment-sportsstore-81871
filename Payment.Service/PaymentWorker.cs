using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Shared.Contracts.Events;

namespace Payment.Service;

public class PaymentWorker : BackgroundService
{
    private readonly ILogger<PaymentWorker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly Random _random = new();

    // Test amounts that always fail (simulate declined cards)
    private static readonly decimal[] AlwaysFailAmounts = { 1111.11m, 2222.22m };

    public PaymentWorker(ILogger<PaymentWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel == null) return;

        const string exchange = "payment.requested";
        const string resultExchange = "payment.processed";
        const string queueName = "payment-service.requested";

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
                var request = JsonSerializer.Deserialize<PaymentProcessingRequested>(json);
                if (request == null) return;

                _logger.LogInformation(
                    "PaymentProcessingRequested for Order {OrderId}, Amount={Amount}, CustomerId={CustomerId}, CorrelationId={CorrelationId}",
                    request.OrderId, request.Amount, request.CustomerId, request.CorrelationId);

                // Simulate payment processing delay
                await Task.Delay(2000, stoppingToken);

                bool approved;
                string? transactionId = null;
                string? rejectionReason = null;

                // Specific amounts always fail (test scenario)
                if (AlwaysFailAmounts.Contains(request.Amount))
                {
                    approved = false;
                    rejectionReason = "Card declined (test card)";
                    _logger.LogWarning("Payment DECLINED for Order {OrderId} — test amount {Amount}", request.OrderId, request.Amount);
                }
                // 80% approval rate otherwise
                else if (_random.NextDouble() < 0.80)
                {
                    approved = true;
                    transactionId = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
                    _logger.LogInformation("Payment APPROVED for Order {OrderId}, TransactionId={TransactionId}", request.OrderId, transactionId);
                }
                else
                {
                    approved = false;
                    rejectionReason = "Insufficient funds";
                    _logger.LogWarning("Payment REJECTED for Order {OrderId} — insufficient funds", request.OrderId);
                }

                var result = new PaymentProcessed(
                    request.OrderId, request.CorrelationId, approved,
                    transactionId, rejectionReason, DateTime.UtcNow);

                var resultJson = JsonSerializer.Serialize(result);
                var body = Encoding.UTF8.GetBytes(resultJson);
                var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
                await _channel.BasicPublishAsync(resultExchange, "", false, props, body);

                _logger.LogInformation("Published PaymentProcessed for Order {OrderId}, Approved={Approved}", request.OrderId, approved);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Payment.Service listening for messages...");
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
                _logger.LogInformation("Payment.Service connected to RabbitMQ");
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
