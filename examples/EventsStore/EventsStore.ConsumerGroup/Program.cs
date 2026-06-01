// KubeMQ Aspire — EventsStore: Consumer Group
//
// Load-balanced persistent event consumption using consumer groups.
// Each event is delivered to only one member of the group.

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

// Consumer A — group "processors"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventStoreSubscription
        {
            Channel = "store.group",
            Group = "processors",
            StartPosition = EventStoreStartPosition.StartFromNew
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Consumer-A] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Consumer B — same group "processors"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventStoreSubscription
        {
            Channel = "store.group",
            Group = "processors",
            StartPosition = EventStoreStartPosition.StartFromNew
        };
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Consumer-B] Seq={Sequence}: {Body}", ev.Sequence, body);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Publisher
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 10; i++)
        {
            await client.SendEventStoreAsync(new EventStoreMessage
            {
                Channel = "store.group",
                Body = Encoding.UTF8.GetBytes($"Group event #{i}")
            }, stoppingToken);
            await Task.Delay(500, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
