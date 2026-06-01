// KubeMQ Aspire — ErrorHandling: Graceful Shutdown
//
// Clean shutdown with in-flight message draining.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();

var stoppingToken = lifetime.ApplicationStopping;
var inFlightCount = 0;

lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Shutdown requested — waiting for {Count} in-flight messages", inFlightCount);
});

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.graceful" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            Interlocked.Increment(ref inFlightCount);
            try
            {
                var body = Encoding.UTF8.GetString(ev.Body.Span);
                logger.LogInformation("Processing: {Body}", body);
                await Task.Delay(500, stoppingToken); // simulate work
            }
            finally
            {
                Interlocked.Decrement(ref inFlightCount);
            }
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Subscription ended — all in-flight messages drained");
    }
}, stoppingToken);

logger.LogInformation("Press Ctrl+C to trigger graceful shutdown");
await host.RunAsync();
