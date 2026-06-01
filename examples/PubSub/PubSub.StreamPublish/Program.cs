// KubeMQ Aspire — PubSub: Stream Publish
//
// High-throughput event publishing using a bidirectional gRPC stream.
// The stream reuses a single gRPC call for many events, reducing overhead.

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
logger.LogInformation("Connected to KubeMQ");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        // Create the stream with an error handler
        await using var stream = await client.CreateEventStreamAsync(
            onError: ex => logger.LogError(ex, "Stream error"),
            cancellationToken: stoppingToken);

        // Send 100 events via the stream
        for (var i = 1; i <= 100; i++)
        {
            await stream.SendAsync(new EventMessage
            {
                Channel = "events.stream",
                Body = Encoding.UTF8.GetBytes($"Stream event #{i}")
            }, "stream-publisher", stoppingToken);

            if (i % 25 == 0)
                logger.LogInformation("Sent {Count} events via stream", i);
        }

        await stream.CloseAsync();
        logger.LogInformation("Stream closed — sent 100 events");
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
