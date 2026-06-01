// KubeMQ Aspire — EventsStore: Replay From Time
//
// Replays events from a specific historical timestamp.
// Similar to StartAtTime but emphasizes the replay scenario.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.EventsStore;

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

_ = Task.Run(async () =>
{
    try
    {
        // Capture "replay start" time before publishing
        var replayFrom = DateTimeOffset.UtcNow;

        await Task.Delay(500, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.replaytime",
                Body = Encoding.UTF8.GetBytes($"Replay event #{i}")
            }, stoppingToken);
            await Task.Delay(300, stoppingToken);
        }
        logger.LogInformation("Published 5 events — replaying from {Time}", replayFrom);

        // Replay from the captured time
        var subscription = new EventStoreSubscription
        {
            Channel = "store.replaytime",
            StartPosition = EventStoreStartPosition.StartAtTime,
            StartTime = replayFrom
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Replay] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
