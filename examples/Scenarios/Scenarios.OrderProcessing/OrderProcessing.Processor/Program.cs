// KubeMQ Aspire — Scenario: Order Processing — Processor
//
// Processes orders from queue and publishes status events.

using System.Text;
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
logger.LogInformation("Order processor started");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
            {
                Channel = "queues.orders",
                MaxMessages = 5,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);

            foreach (var msg in response.Messages)
            {
                var body = Encoding.UTF8.GetString(msg.Body.Span);
                logger.LogInformation("[Processor] Processing order: {Body}", body);

                // Publish status event
                await client.SendEventAsync(new EventMessage
                {
                    Channel = "events.order-status",
                    Body = Encoding.UTF8.GetBytes($"processed:{body}")
                }, stoppingToken);
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
