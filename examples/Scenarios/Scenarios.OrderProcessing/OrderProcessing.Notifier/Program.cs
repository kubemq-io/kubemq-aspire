// KubeMQ Aspire — Scenario: Order Processing — Notifier
//
// Subscribes to order status events and sends notifications.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Notifier started — listening for order status events");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.order-status" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Notifier] Order status update: {Body}", body);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
