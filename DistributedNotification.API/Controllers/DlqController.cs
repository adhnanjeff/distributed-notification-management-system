using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DistributedNotification.API.Controllers
{
    [ApiController]
    [Route("api/dlq")]
    public class DlqController : ControllerBase
    {
        private readonly ServiceBusClient _client;
        private readonly IConfiguration _config;

        public DlqController(
            ServiceBusClient client,
            IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        [HttpGet("{subscription}")]
        public async Task<IActionResult> PeekDlq(string subscription)
        {
            try
            {
                await using var receiver = _client.CreateReceiver(
                    _config["ServiceBus:TopicName"],
                    subscription,
                    new ServiceBusReceiverOptions
                    {
                        SubQueue = SubQueue.DeadLetter
                    }
                );

                var messages = await receiver.PeekMessagesAsync(10);

                var result = messages.Select(m => new
                {
                    messageId = m.MessageId,
                    body = m.Body.ToString(),
                    reason = m.DeadLetterReason,
                    error = m.DeadLetterErrorDescription,
                    deliveryCount = m.DeliveryCount
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new object[0]);
            }
        }

        [HttpPost("replay/{subscription}")]
        public async Task<IActionResult> ReplayDlq(string subscription)
        {
            try
            {
                await using var client = new ServiceBusClient(_config["ServiceBus:ConnectionString"]);
                await using var receiver = client.CreateReceiver(
                    _config["ServiceBus:TopicName"],
                    subscription,
                    new ServiceBusReceiverOptions
                    {
                        SubQueue = SubQueue.DeadLetter
                    }
                );
                await using var sender = client.CreateSender(_config["ServiceBus:TopicName"]);

                var messages = await receiver.ReceiveMessagesAsync(10);

                foreach (var msg in messages)
                {
                    var replayMessage = new ServiceBusMessage(msg.Body)
                    {
                        ContentType = msg.ContentType,
                        MessageId = Guid.NewGuid().ToString()
                    };

                    foreach (var prop in msg.ApplicationProperties)
                    {
                        replayMessage.ApplicationProperties[prop.Key] = prop.Value;
                    }

                    await sender.SendMessageAsync(replayMessage);
                    await receiver.CompleteMessageAsync(msg);
                }

                return Ok("DLQ messages replayed");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to replay messages: {ex.Message}");
            }
        }
    }
}
