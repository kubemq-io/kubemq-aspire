// KubeMQ Aspire — EventsStore: Replay From Sequence
//
// Starts replaying events from a specific sequence number.

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
        // Publish events to build up sequence numbers
        for (var i = 1; i <= 10; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.sequence",
                Body = Encoding.UTF8.GetBytes($"Event #{i}")
            }, stoppingToken);
        }
        logger.LogInformation("Published 10 events — replaying from sequence 5");

        await Task.Delay(1000, stoppingToken);
        // Subscribe from sequence 5 — gets events 5, 6, 7, 8, 9, 10
        var subscription = new EventStoreSubscription
        {
            Channel = "store.sequence",
            StartPosition = EventStoreStartPosition.StartAtSequence,
            StartSequence = 5
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[FromSeq] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
