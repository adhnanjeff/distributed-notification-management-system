using System;

namespace DistributedNotification.Core.Entities;

public class NotificationMessage
{
    public Guid Id { get; set; }
    public string? Type { get; set; }      // OrderPlaced, PaymentFailed
    public string? Channel { get; set; }   // Email, SMS, Push
    public string? UserId { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }    // PENDING, SENT, FAILED
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid CorrelationId { get; set; } = Guid.Empty;
    public string? TenantId { get; set; }
}