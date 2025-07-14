using EFDemo.Data;
using EFDemo.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly ConnectionFactory _factory;

    public OutboxPublisherService(IServiceProvider serviceProvider, ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _factory = new ConnectionFactory { HostName = "localhost" }; // Use config if needed
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherService started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                _logger.LogInformation("Polling for unsent outbox messages...");
                var unsent = db.OutboxMessages
                    .Where(m => !m.IsProcessed)
                    .OrderBy(m => m.OccurredOn)
                    .Take(5)
                    .ToList();

                if (unsent.Count > 0)
                {
                    _logger.LogInformation($"Found {unsent.Count} unsent outbox messages.");
                    using var connection = _factory.CreateConnection("OutboxPublisher");
                    using var channel = connection.CreateModel();
                    channel.ExchangeDeclare(exchange: "student", type: ExchangeType.Fanout, durable: true);

                    foreach (var msg in unsent)
                    {
                        _logger.LogInformation($"Publishing OutboxMessage {msg.Id} of type {msg.Type} to RabbitMQ. Payload: {msg.Payload}");
                        var body = Encoding.UTF8.GetBytes(msg.Payload);
                        channel.BasicPublish(exchange: "student", routingKey: "", basicProperties: null, body: body);
                        msg.IsProcessed = true;
                        msg.ProcessedOn = DateTime.UtcNow;
                        _logger.LogInformation($"Marked OutboxMessage {msg.Id} as processed.");
                    }
                    db.SaveChanges();
                    _logger.LogInformation("Saved changes to OutboxMessages.");
                }
                else
                {
                    _logger.LogInformation("No unsent outbox messages found.");
                }

                // Cleanup processed messages older than 30 days, once per hour
                var now = DateTime.UtcNow;
                if (now.Minute == 0) // crude hourly check
                {
                    var cutoff = now.AddDays(-30);
                    var oldProcessed = db.OutboxMessages
                        .Where(m => m.IsProcessed && m.ProcessedOn < cutoff)
                        .ToList();
                    if (oldProcessed.Count > 0)
                    {
                        db.OutboxMessages.RemoveRange(oldProcessed);
                        db.SaveChanges();
                        _logger.LogInformation($"Cleaned up {oldProcessed.Count} processed OutboxMessages older than 30 days.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OutboxPublisherService loop.");
            }
            await Task.Delay(2000, stoppingToken);
        }
        _logger.LogInformation("OutboxPublisherService stopping.");
    }
} 