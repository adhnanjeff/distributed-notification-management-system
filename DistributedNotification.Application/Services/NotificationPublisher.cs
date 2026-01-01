using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using DistributedNotification.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DistributedNotification.Core.Interfaces;
namespace DistributedNotification.Application.Services;

public class NotificationPublisher : INotificationPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(IConfiguration config, ILogger<NotificationPublisher> logger)
    {
        _logger = logger;
        var client = new ServiceBusClient(
            Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING") ?? config["ServiceBus:ConnectionString"]
        );

        _sender = client.CreateSender(
            Environment.GetEnvironmentVariable("SERVICEBUS_TOPIC_NAME") ?? config["ServiceBus:TopicName"]
        );
    }

    public async Task PublishAsync(NotificationMessage message)
    {
        
        _logger.LogInformation("🚀 Publishing notification {Id} to Service Bus", message.Id);
        
        var payload = new 
        {
            NotificationId = message.Id,
            Type = message.Type,
            Channel = message.Channel,
            UserId = message.UserId,
            Message = message.Message,
            TenantId = message.TenantId
        };
        
        var json = JsonSerializer.Serialize(payload);
        _logger.LogInformation("📝 Payload: {Payload}", json);

        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = message.Id.ToString()
        };

        serviceBusMessage.ApplicationProperties["TenantId"] = payload.TenantId;
        // Set Channel as message property for Azure Service Bus filtering
        serviceBusMessage.ApplicationProperties["Channel"] = message.Channel;
        serviceBusMessage.CorrelationId = message.Id.ToString();

        
        try
        {
            await _sender.SendMessageAsync(serviceBusMessage);
            _logger.LogInformation("✅ Message {Id} sent to Service Bus successfully", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send message {Id} to Service Bus", message.Id);
            throw;
        }
    }
}
