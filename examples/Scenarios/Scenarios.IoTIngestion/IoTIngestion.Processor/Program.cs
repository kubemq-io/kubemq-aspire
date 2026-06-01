// KubeMQ Aspire — Scenario: IoT Ingestion — Processor
//
// Processes sensor data and publishes alerts for high values.

using System.Text;
using System.Text.Json;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;
using KubeMQ.Sdk.Queues;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("IoT Processor started");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.iot.sensors" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            var data = JsonSerializer.Deserialize<JsonElement>(body);
            var value = data.GetProperty("value").GetDouble();

            logger.LogInformation("[Processor] Value={Value:F1}", value);

            // If temperature > 40, send alert to queue
            if (value > 40)
            {
                await client.SendQueueMessageAsync(new QueueMessage
                {
                    Channel = "queues.iot.alerts",
                    Body = Encoding.UTF8.GetBytes($"HIGH TEMP ALERT: {value:F1}C")
                }, stoppingToken);
                logger.LogWarning("[Processor] Alert queued: {Value:F1}C", value);
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
