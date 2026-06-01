// KubeMQ Aspire — EventsStore: Start At Time
//
// Replays events stored from a specific UTC timestamp forward.

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
        // Publish some events
        for (var i = 1; i <= 3; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.attime",
                Body = Encoding.UTF8.GetBytes($"Before-timestamp event #{i}")
            }, stoppingToken);
        }

        // Record a timestamp, then publish more events
        await Task.Delay(1000, stoppingToken);
        var startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("Timestamp marker: {Time}", startTime);

        await Task.Delay(500, stoppingToken);
        for (var i = 1; i <= 3; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.attime",
                Body = Encoding.UTF8.GetBytes($"After-timestamp event #{i}")
            }, stoppingToken);
        }

        // Subscribe from the timestamp — only gets "after" events
        var subscription = new EventStoreSubscription
        {
            Channel = "store.attime",
            StartPosition = EventStoreStartPosition.StartAtTime,
            StartTime = startTime
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[AtTime] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
