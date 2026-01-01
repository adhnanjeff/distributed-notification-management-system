using DistributedNotification.Core.Entities;

namespace DistributedNotification.Core.Interfaces;

public interface INotificationPublisher
{
     Task PublishAsync(NotificationMessage notification);
}
