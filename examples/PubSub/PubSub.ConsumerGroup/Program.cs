// KubeMQ Aspire — PubSub: Consumer Group
//
// Multiple subscribers with the same Group — only ONE receives each message.
// This demonstrates load-balanced event consumption.

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

// Worker A — same group "workers"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription
        {
            Channel = "events.group",
            Group = "workers" // group name for load balancing
        };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Worker-A] Received: {Body}", body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Worker B — same group "workers"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription
        {
            Channel = "events.group",
            Group = "workers"
        };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Worker-B] Received: {Body}", body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Publisher — sends 10 events; each will go to only one worker
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 10; i++)
        {
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.group",
                Body = Encoding.UTF8.GetBytes($"Task #{i}")
            }, stoppingToken);
            logger.LogInformation("Published task #{Counter}", i);
            await Task.Delay(500, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
