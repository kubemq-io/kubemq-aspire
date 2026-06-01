// KubeMQ Aspire — PubSub: Multiple Subscribers
//
// Two subscribers on the same channel — both receive every message.
// This demonstrates the fan-out behavior of PubSub events.

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

var stoppingToken = lifetime.ApplicationStopping;

// Subscriber A
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.multiple" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Sub-A] Received: {Body}", body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Subscriber B
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.multiple" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Sub-B] Received: {Body}", body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Publisher
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.multiple",
                Body = Encoding.UTF8.GetBytes($"Broadcast #{i}")
            }, stoppingToken);
            logger.LogInformation("Published event #{Counter}", i);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
