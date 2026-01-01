using Azure.Identity;
using DistributedNotification.SmsWorker;
using DistributedNotification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load("../.env");

var builder = Host.CreateApplicationBuilder(args);

// TODO: Enable Key Vault when deployed to Azure
// builder.Configuration.AddAzureKeyVault(
//     new Uri("https://kv-notification-system.vault.azure.net/"),
//     new DefaultAzureCredential()
// );

builder.Services.AddHostedService<Worker>();

// ✅ Register DbContext (REQUIRED)
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlite(
        "Data Source=../DistributedNotification.API/notifications.db",
        b => b.MigrationsAssembly("DistributedNotification.Infrastructure")
    ));

var host = builder.Build();
host.Run();

