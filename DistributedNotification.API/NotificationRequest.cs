using System;

namespace DistributedNotification.API;

public class NotificationRequest
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Type { get; set; }
    public string UserId { get; set; }
    public string Channel { get; set; }
    public string Message { get; set; }
    public Guid CorrelationId { get; set; } = Guid.Empty;
    public string TenantId { get; set; }
}
