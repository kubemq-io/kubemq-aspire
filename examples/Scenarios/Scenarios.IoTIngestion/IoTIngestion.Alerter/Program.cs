// KubeMQ Aspire — Scenario: IoT Ingestion — Alerter
//
// Consumes alerts from queue for guaranteed delivery.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("IoT Alerter started — polling for alerts");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
            {
                Channel = "queues.iot.alerts",
                MaxMessages = 5,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);

            foreach (var msg in response.Messages)
            {
                var body = Encoding.UTF8.GetString(msg.Body.Span);
                logger.LogWarning("[ALERT] {Body}", body);
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
