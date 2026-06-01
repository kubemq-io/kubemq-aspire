// KubeMQ Aspire — Patterns: Work Queue
//
// Competing consumers on a queue — tasks are distributed among workers.

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
var stoppingToken = lifetime.ApplicationStopping;

// Produce 10 tasks
for (var i = 1; i <= 10; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "queues.workqueue",
        Body = Encoding.UTF8.GetBytes($"Task #{i}")
    });
}
logger.LogInformation("Produced 10 tasks");

// Worker A
_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
            {
                Channel = "queues.workqueue",
                MaxMessages = 1,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);
            foreach (var msg in response.Messages)
            {
                logger.LogInformation("[Worker-A] {Body}", Encoding.UTF8.GetString(msg.Body.Span));
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Worker B
_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
            {
                Channel = "queues.workqueue",
                MaxMessages = 1,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);
            foreach (var msg in response.Messages)
            {
                logger.LogInformation("[Worker-B] {Body}", Encoding.UTF8.GetString(msg.Body.Span));
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
