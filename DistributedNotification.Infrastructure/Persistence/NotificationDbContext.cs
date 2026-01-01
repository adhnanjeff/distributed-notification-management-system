using System;
using DistributedNotification.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedNotification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions options) : base(options) { }

    public DbSet<NotificationMessage> Notifications { get; set; }
}

