using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DistributedNotification.Application.Services;
using DistributedNotification.Core.Interfaces;
using DistributedNotification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// TODO: Enable Key Vault when deployed to Azure
// builder.Configuration.AddAzureKeyVault(
//     new Uri("https://kv-notification-system.vault.azure.net/"),
//     new DefaultAzureCredential()
// );

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlite(
        "Data Source=notifications.db",
        b => b.MigrationsAssembly("DistributedNotification.Infrastructure")
    ));

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ServiceBusClient(
        config["ServiceBus:ConnectionString"]
    );
});

// API Key middleware for basic authentication
var apiKey = builder.Configuration["ApiKey"] ?? "secure-demo-key";

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
