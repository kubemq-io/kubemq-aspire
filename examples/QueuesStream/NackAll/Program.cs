// KubeMQ Aspire — QueuesStream: Nack All
//
// Rejects all messages in a batch — they return to the queue for redelivery.

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

for (var i = 1; i <= 3; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.nackall",
        Body = Encoding.UTF8.GetBytes($"Nack-all #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.nackall",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    logger.LogInformation("Received {Count} messages — nacking all", batch.Messages.Count);
    // Batch-level nack — all messages return to queue
    await batch.NackAllAsync();
    logger.LogInformation("All messages rejected (returned to queue)");
}

await host.RunAsync();
