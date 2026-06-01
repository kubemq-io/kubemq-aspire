// KubeMQ Aspire — Aspire: Keyed Multi-Instance
//
// Registers two separate KubeMQ connections using AddKeyedKubeMQClient().

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Register two KubeMQ connections with different keys
builder.AddKeyedKubeMQClient("orders");
builder.AddKeyedKubeMQClient("notifications");

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Resolve by key
var ordersClient = host.Services.GetRequiredKeyedService<IKubeMQClient>("orders");
var notifClient = host.Services.GetRequiredKeyedService<IKubeMQClient>("notifications");

await ordersClient.ConnectAsync();
await notifClient.ConnectAsync();
logger.LogInformation("Both keyed clients connected");

// Use each client independently
await ordersClient.SendEventAsync(new EventMessage
{
    Channel = "events.orders",
    Body = Encoding.UTF8.GetBytes("New order created")
});
logger.LogInformation("Sent order event via 'orders' client");

await notifClient.SendEventAsync(new EventMessage
{
    Channel = "events.notifications",
    Body = Encoding.UTF8.GetBytes("User notified")
});
logger.LogInformation("Sent notification via 'notifications' client");

await host.RunAsync();
