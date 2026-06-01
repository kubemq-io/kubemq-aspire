// KubeMQ Aspire — PubSub: Wildcard Subscription
//
// Subscribes using a wildcard pattern "events.*" to receive events
// published to any channel matching the pattern (events.basic, events.test, etc.).

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
logger.LogInformation("Connected to KubeMQ — subscribing to events.*");

var stoppingToken = lifetime.ApplicationStopping;

// First, publish to different sub-channels
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        var channels = new[] { "events.orders", "events.notifications", "events.logs" };
        for (var i = 0; i < 6; i++)
        {
            var channel = channels[i % channels.Length];
            await client.SendEventAsync(new EventMessage
            {
                Channel = channel,
                Body = Encoding.UTF8.GetBytes($"Message #{i + 1} to {channel}")
            }, stoppingToken);
            logger.LogInformation("Published to {Channel}", channel);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Subscribe with wildcard — receives from all events.* channels
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.*" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Wildcard] Received on {Channel}: {Body}", ev.Channel, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
