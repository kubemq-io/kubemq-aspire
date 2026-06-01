// KubeMQ Aspire — EventsStore: Start At Time Delta
//
// Starts from a relative time offset (e.g., events from the last 60 seconds).

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

// Publish some events, then subscribe from 60 seconds ago
_ = Task.Run(async () =>
{
    try
    {
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.timedelta",
                Body = Encoding.UTF8.GetBytes($"Recent event #{i}")
            }, stoppingToken);
            await Task.Delay(500, stoppingToken);
        }

        logger.LogInformation("Published 5 events — subscribing from last 60 seconds");

        var subscription = new EventStoreSubscription
        {
            Channel = "store.timedelta",
            StartPosition = EventStoreStartPosition.StartAtTimeDelta,
            StartTimeDeltaSeconds = 60 // events from last 60 seconds
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[TimeDelta] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
