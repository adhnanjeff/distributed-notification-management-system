using DistributedNotification.Core.Interfaces;
using DistributedNotification.Core.Entities;
using DistributedNotification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DistributedNotification.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationPublisher _publisher;
        private readonly NotificationDbContext _db;

        public NotificationsController(INotificationPublisher publisher, NotificationDbContext db)
        {
            _publisher = publisher;
            _db = db;
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus(Guid id)
        {
            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound();

            return Ok(new
            {
                notification.Id,
                notification.Status,
                notification.ProcessedAt
            });
        }


        [HttpPost]
        public async Task<IActionResult> Send(NotificationRequest request)
        {
            var notification = new NotificationMessage
            {
                Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
                Type = request.Type,
                Channel = request.Channel,
                UserId = request.UserId,
                Message = request.Message,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                CorrelationId = request.CorrelationId == Guid.Empty ? Guid.NewGuid() : request.CorrelationId,
                TenantId = request.TenantId
            };

            // Save to database first
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Then publish to Service Bus
            await _publisher.PublishAsync(notification);

            return Ok(new { notification.Id });
        }
    }
}
