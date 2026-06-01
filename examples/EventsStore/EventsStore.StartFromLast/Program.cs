// KubeMQ Aspire — EventsStore: Start From Last
//
// Starts from the last stored event and receives new ones going forward.

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

// Publish some history first
_ = Task.Run(async () =>
{
    try
    {
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.last",
                Body = Encoding.UTF8.GetBytes($"Old event #{i}")
            }, stoppingToken);
        }
        logger.LogInformation("Published 5 historical events");

        // Subscribe from last — gets only the last stored event plus new ones
        await Task.Delay(1000, stoppingToken);
        var subscription = new EventStoreSubscription
        {
            Channel = "store.last",
            StartPosition = EventStoreStartPosition.StartFromLast
        };

        var subTask = Task.Run(async () =>
        {
            await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
            {
                var body = Encoding.UTF8.GetString(ev.Body.Span);
                logger.LogInformation("[FromLast] Seq={Sequence}: {Body}", ev.Sequence, body);
            }
        }, stoppingToken);

        // Publish new events after subscribing
        await Task.Delay(1000, stoppingToken);
        for (var i = 1; i <= 3; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.last",
                Body = Encoding.UTF8.GetBytes($"New event #{i}")
            }, stoppingToken);
            await Task.Delay(500, stoppingToken);
        }

        await subTask;
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
