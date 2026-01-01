namespace DistributedNotification.PushWorker;

using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using DistributedNotification.Infrastructure.Persistence;
using System.Text.Json;

public class Worker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        IConfiguration config,
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        var client = new ServiceBusClient(
            Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING") ?? config["ServiceBus:ConnectionString"]
        );

        _processor = client.CreateProcessor(
            Environment.GetEnvironmentVariable("SERVICEBUS_TOPIC_NAME") ?? config["ServiceBus:TopicName"],
            config["ServiceBus:SubscriptionName"],
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 20,
                AutoCompleteMessages = false
            }
        );
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessage;
        _processor.ProcessErrorAsync += ProcessError;

        _logger.LogInformation("🚀 Starting PushWorker processor...");
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("✅ PushWorker processor started successfully");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var payload = JsonSerializer.Deserialize<NotificationPayload>(body);

            // Only process PUSH notifications
            if (payload?.Channel != "Push")
            {
                _logger.LogInformation("🔔 Push Worker skipping {Channel} notification {Id}", payload?.Channel, payload?.NotificationId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            _logger.LogInformation(
                "Processing notification {NotificationId} | CorrelationId {CorrelationId}",
                payload.NotificationId,
                payload.CorrelationId
            );


            // Simulate push notification sending (reduced from 500ms to 20ms)
            await Task.Delay(20);

            // ✅ Create a scope PER MESSAGE
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var notification = await db.Notifications
                .FindAsync(payload.NotificationId);

            if (notification != null)
            {
                _logger.LogInformation("🔄 Updating notification {Id} to SENT", payload.NotificationId);
                notification.Status = "SENT";
                notification.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                _logger.LogInformation("✅ Notification {Id} marked as SENT", payload.NotificationId);
            }
            else
            {
                _logger.LogWarning("⚠️ Notification {Id} not found in database", payload.NotificationId);
            }
            


            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Push Worker failed");

            // Optional: mark FAILED
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var body = args.Message.Body.ToString();
            var payload = JsonSerializer.Deserialize<NotificationPayload>(body);

            var notification = await db.Notifications
                .FindAsync(payload!.NotificationId);

            if (notification != null)
            {
                notification.Status = "FAILED";
                await db.SaveChangesAsync();
            }

            throw; // let Service Bus retry
        }
    }



    private Task ProcessError(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "❌ Error processing message");
        return Task.CompletedTask;
    }
}

