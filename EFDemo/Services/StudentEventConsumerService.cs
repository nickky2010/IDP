using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

public class StudentEventConsumerService : BackgroundService
{
    private readonly ILogger<StudentEventConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection _connection;
    private IModel _channel;

    public StudentEventConsumerService(ILogger<StudentEventConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var rabbitConfig = _configuration.GetSection("RabbitMQ");
        var factory = new ConnectionFactory() {
            HostName = rabbitConfig["HostName"],
            UserName = rabbitConfig["UserName"],
            Password = rabbitConfig["Password"]
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "student", type: ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(queue: "student_events", durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: "student_events", exchange: "student", routingKey: "");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"[Consumer] Received message: {message}");
            try
            {
                var json = JObject.Parse(message);
                // Simulate edge case: if City == 'Rollback City', throw
                if ((string?)json["City"] == "Rollback City")
                {
                    throw new System.Exception("Simulated processing failure for rollback test.");
                }
                // Simulate normal processing
                _logger.LogInformation($"[Consumer] Processed StudentCreated event for Student Id={json["Id"]}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"[Consumer] Error processing message: {message}");
                // In a real system, you might NACK or requeue the message here
            }
        };
        _channel.BasicConsume(queue: "student_events", autoAck: true, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
} 