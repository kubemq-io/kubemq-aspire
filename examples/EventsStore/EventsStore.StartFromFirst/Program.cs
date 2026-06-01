// KubeMQ Aspire — EventsStore: Start From First
//
// Replays ALL stored events from the very beginning of the channel.

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

// First, publish some events to have data to replay
_ = Task.Run(async () =>
{
    try
    {
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.first",
                Body = Encoding.UTF8.GetBytes($"Historical event #{i}")
            }, stoppingToken);
        }
        logger.LogInformation("Published 5 events to store.first");

        // Now subscribe from the first event — replays all history
        await Task.Delay(1000, stoppingToken);
        var subscription = new EventStoreSubscription
        {
            Channel = "store.first",
            StartPosition = EventStoreStartPosition.StartFromFirst
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
