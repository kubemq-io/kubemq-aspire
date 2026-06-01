// KubeMQ Aspire — QueuesStream: Ack All
//
// Acknowledges all messages in a batch at once using batch-level settlement.

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

for (var i = 1; i <= 5; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.ackall",
        Body = Encoding.UTF8.GetBytes($"Ack-all #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.ackall",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    logger.LogInformation("Received {Count} messages — acknowledging all at once", batch.Messages.Count);
    // Batch-level ack — acknowledges every message in the batch
    await batch.AckAllAsync();
    logger.LogInformation("All messages acknowledged");
}

await host.RunAsync();
