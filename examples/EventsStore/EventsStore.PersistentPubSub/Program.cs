// KubeMQ Aspire — EventsStore: Persistent Pub/Sub
//
// Publishes persistent events and subscribes to replay them.
// Unlike plain Events, EventsStore events are stored and can be replayed.

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

// Subscriber — starts receiving new events only
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventStoreSubscription
        {
            Channel = "store.persistent",
            StartPosition = EventStoreStartPosition.StartFromNew
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Store] Seq={Sequence}, Body={Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Publisher — sends persistent events
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            var result = await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.persistent",
                Body = Encoding.UTF8.GetBytes($"Persistent event #{i}"),
                Tags = new Dictionary<string, string> { ["source"] = "persistent-pubsub" }
            }, stoppingToken);
            logger.LogInformation("Stored event #{Counter}, Sent={Sent}", i, result.Sent);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
