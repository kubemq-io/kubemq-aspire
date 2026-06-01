// KubeMQ Aspire — Aspire: Data Volume
//
// Demonstrates persistent storage with WithDataVolume().
// Messages survive container restarts.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.EventsStore;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected — AppHost uses WithDataVolume() for persistent storage");

// Publish a persistent event
await client.SendEventStoreAsync(new EventStoreMessage
{
    Channel = "store.volume",
    Body = Encoding.UTF8.GetBytes($"Persisted at {DateTime.UtcNow}")
});
logger.LogInformation("Sent persistent event — survives container restart");

// Replay from first to show persistence
var subscription = new EventStoreSubscription
{
    Channel = "store.volume",
    StartPosition = EventStoreStartPosition.StartFromFirst
};

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);

_ = Task.Run(async () =>
{
    try
    {
        await foreach (var ev in client.SubscribeToEventsStoreAsync(subscription, cts.Token))
        {
            logger.LogInformation("[Volume] Seq={Sequence}: {Body}",
                ev.Sequence, Encoding.UTF8.GetString(ev.Body.Span));
        }
    }
    catch (OperationCanceledException) { }
}, cts.Token);

await host.RunAsync();
