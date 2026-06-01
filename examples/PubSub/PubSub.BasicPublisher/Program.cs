// KubeMQ Aspire — PubSub: Basic Publisher
//
// Publishes an event to the "events.basic" channel every 2 seconds.
// Events are fire-and-forget — no delivery guarantee, no persistence.

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
var counter = 0;

_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            counter++;
            // Send a fire-and-forget event
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.basic",
                Body = Encoding.UTF8.GetBytes($"Hello from publisher #{counter}"),
                Tags = new Dictionary<string, string> { ["source"] = "basic-publisher" }
            }, stoppingToken);

            logger.LogInformation("Published event #{Counter} to events.basic", counter);
            await Task.Delay(2000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
