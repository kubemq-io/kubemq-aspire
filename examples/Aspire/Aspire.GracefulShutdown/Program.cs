// KubeMQ Aspire — Aspire: Graceful Shutdown
//
// Demonstrates proper lifecycle management — the KubeMQ client is disposed
// automatically by the DI container when the host shuts down.

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
logger.LogInformation("Connected to KubeMQ");

lifetime.ApplicationStopping.Register(() =>
    logger.LogInformation("Application stopping — KubeMQ client will be disposed by DI"));

lifetime.ApplicationStopped.Register(() =>
    logger.LogInformation("Application stopped — cleanup complete"));

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.shutdown" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            logger.LogInformation("Received: {Body}", Encoding.UTF8.GetString(ev.Body.Span));
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Subscription cancelled gracefully on shutdown");
    }
}, stoppingToken);

logger.LogInformation("Press Ctrl+C to trigger graceful shutdown");
await host.RunAsync();
