// KubeMQ Aspire — Queues: Dead Letter Queue
//
// Messages that exceed MaxReceiveCount are routed to a dead letter queue.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ");

// Send a message with max receive count = 2 and DLQ channel
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.dlq",
    Body = Encoding.UTF8.GetBytes("Poison message — will fail processing"),
    MaxReceiveCount = 2,
    MaxReceiveQueue = "queues.dlq.dead" // dead letter channel
});
logger.LogInformation("Sent poison message with MaxReceiveCount=2");

// Simulate failing to process — receive and nack twice
await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

for (var attempt = 1; attempt <= 2; attempt++)
{
    var batch = await receiver.PollAsync(new QueuePollRequest
    {
        Channel = "queues.dlq",
        MaxMessages = 1,
        WaitTimeoutSeconds = 5,
        AutoAck = false
    });

    if (batch.HasMessages)
    {
        foreach (var msg in batch.Messages)
        {
            logger.LogInformation("Attempt {Attempt}: rejecting message", attempt);
            await msg.NackAsync();
        }
    }
}

// Check the dead letter queue
await Task.Delay(1000);
var dlqResponse = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.dlq.dead",
    MaxMessages = 1,
    WaitTimeoutSeconds = 5,
    AutoAck = true
});

if (dlqResponse.HasMessages)
{
    foreach (var msg in dlqResponse.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("[DLQ] Received dead letter: {Body}", body);
    }
}

await host.RunAsync();
