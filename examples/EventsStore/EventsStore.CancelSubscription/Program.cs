// KubeMQ Aspire — EventsStore: Cancel Subscription
//
// Subscribes to a store channel, receives N messages, then cancels.

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
const int maxMessages = 3;

_ = Task.Run(async () =>
{
    try
    {
        // Publish events
        for (var i = 1; i <= 10; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.cancel",
                Body = Encoding.UTF8.GetBytes($"Store event #{i}")
            }, stoppingToken);
        }

        await Task.Delay(1000, stoppingToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var count = 0;

        var subscription = new EventStoreSubscription
        {
            Channel = "store.cancel",
            StartPosition = EventStoreStartPosition.StartFromFirst
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, cts.Token))
        {
            count++;
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[{Count}/{Max}] Seq={Sequence}: {Body}",
                count, maxMessages, ev.Sequence, body);

            if (count >= maxMessages)
            {
                logger.LogInformation("Received {Max} events — cancelling", maxMessages);
                cts.Cancel();
            }
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Store subscription cancelled gracefully");
    }
}, stoppingToken);

await host.RunAsync();
