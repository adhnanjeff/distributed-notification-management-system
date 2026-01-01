namespace DistributedNotification.SmsWorker;

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

        var connectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING") ?? config["ServiceBus:ConnectionString"];
        var topicName = Environment.GetEnvironmentVariable("SERVICEBUS_TOPIC_NAME") ?? config["ServiceBus:TopicName"];
        var subscriptionName = config["ServiceBus:SubscriptionName"];
        
        _logger.LogInformation("📱 SMS Worker connecting to topic: {Topic}, subscription: {Subscription}", topicName, subscriptionName);

        var client = new ServiceBusClient(connectionString);

        _processor = client.CreateProcessor(
            topicName,
            subscriptionName,
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

        _logger.LogInformation("🚀 Starting SmsWorker processor...");
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("✅ SmsWorker processor started successfully");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        _logger.LogInformation("📬 SMS Worker received message: {MessageId}", args.Message.MessageId);
        
        try
        {
            var body = args.Message.Body.ToString();
            _logger.LogInformation("📱 SMS Worker message body: {Body}", body);
            
            var payload = JsonSerializer.Deserialize<NotificationPayload>(body);

            // Only process SMS notifications
            if (payload?.Channel != "SMS")
            {
                _logger.LogInformation("📱 SMS Worker skipping {Channel} notification {Id}", payload?.Channel, payload?.NotificationId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            _logger.LogInformation(
                "Processing notification {NotificationId} | CorrelationId {CorrelationId}",
                payload.NotificationId,
                payload.CorrelationId
            );


            // Simulate SMS sending (reduced from 500ms to 30ms)
            await Task.Delay(30);

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
            _logger.LogError(ex, "❌ SMS Worker failed");

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

