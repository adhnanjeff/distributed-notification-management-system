namespace DistributedNotification.EmailWorker;

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

        _logger.LogInformation("🚀 Starting EmailWorker processor...");
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("✅ EmailWorker processor started successfully");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        _logger.LogInformation("📬 Email Worker received message: {MessageId}", args.Message.MessageId);
        
        try
        {
            var body = args.Message.Body.ToString();
            _logger.LogInformation("📧 Email Worker message body: {Body}", body);
            
            var payload = JsonSerializer.Deserialize<NotificationPayload>(body);

            // Only process EMAIL notifications
            if (payload?.Channel != "Email")
            {
                _logger.LogInformation("📧 Email Worker skipping {Channel} notification {Id}", payload?.Channel, payload?.NotificationId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            _logger.LogInformation(
                "Processing notification {NotificationId} | CorrelationId {CorrelationId}",
                payload.NotificationId,
                payload.CorrelationId
            );


            // 🧪 SIMULATE FAILURE FOR TESTING
            if (payload.Message?.Contains("FAIL") == true)
            {
                throw new Exception("Simulated failure");
            }

            // Simulate email sending (reduced from 500ms to 50ms)
            await Task.Delay(50);

            // ✅ Create a scope PER MESSAGE
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var notification = await db.Notifications
                .FindAsync(payload.NotificationId);

            if (notification == null)
            {
                _logger.LogWarning("📋 Notification {Id} not found in database", payload.NotificationId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            _logger.LogInformation("📋 Found notification {Id} with status: {Status}", payload.NotificationId, notification.Status);
            
            // 🔐 IDEMPOTENCY CHECK
            if (notification.Status == "SENT")
            {
                _logger.LogInformation(
                    "🔄 IDEMPOTENCY: Duplicate message ignored for {Id} - already SENT at {ProcessedAt}",
                    payload.NotificationId, notification.ProcessedAt
                );

                await args.CompleteMessageAsync(args.Message);
                return;
            }

            else if (notification != null)
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
            _logger.LogError(ex, "❌ Email Worker failed");

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

