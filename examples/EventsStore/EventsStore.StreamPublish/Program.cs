// KubeMQ Aspire — EventsStore: Stream Publish
//
// High-throughput persistent event publishing using a bidirectional stream.
// Each send awaits server-side persistence confirmation.

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
        await using var stream = await client.CreateEventStoreStreamAsync(stoppingToken);

        for (var i = 1; i <= 50; i++)
        {
            var result = await stream.SendAsync(new EventStoreMessage
            {
                Channel = "store.stream",
                Body = Encoding.UTF8.GetBytes($"Stream store event #{i}")
            }, "store-stream-publisher", stoppingToken);

            if (i % 10 == 0)
                logger.LogInformation("Stored {Count} events, last sent={Sent}", i, result.Sent);
        }

        await stream.CloseAsync();
        logger.LogInformation("Store stream closed — sent 50 events");
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
