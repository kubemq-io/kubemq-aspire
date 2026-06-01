// KubeMQ Aspire — EventsStore: Start New Only
//
// Only receives NEW events — ignores all previously stored history.

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

// Subscribe with StartFromNew — history is ignored
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventStoreSubscription
        {
            Channel = "store.newonly",
            StartPosition = EventStoreStartPosition.StartFromNew
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[NewOnly] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Publish events after subscription is established
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.newonly",
                Body = Encoding.UTF8.GetBytes($"New-only event #{i}")
            }, stoppingToken);
            logger.LogInformation("Published new-only event #{Counter}", i);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
