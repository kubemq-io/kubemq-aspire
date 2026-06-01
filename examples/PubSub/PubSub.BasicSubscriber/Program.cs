// KubeMQ Aspire — PubSub: Basic Subscriber
//
// Subscribes to the "events.basic" channel and logs received events.
// Uses IAsyncEnumerable with await foreach — no callbacks.

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
logger.LogInformation("Connected to KubeMQ — subscribing to events.basic");

var stoppingToken = lifetime.ApplicationStopping;

// Subscribe using await foreach (IAsyncEnumerable)
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.basic" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("Received event on {Channel}: {Body}", ev.Channel, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
