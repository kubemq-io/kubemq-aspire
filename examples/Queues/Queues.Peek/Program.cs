// KubeMQ Aspire — Queues: Peek
//
// Peeks at messages without consuming them — they remain in the queue.

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

// Send messages
for (var i = 1; i <= 3; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "queues.peek",
        Body = Encoding.UTF8.GetBytes($"Peek message #{i}")
    });
}
logger.LogInformation("Sent 3 messages");

// Peek — messages stay in queue
var peeked = await client.PeekQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.peek",
    MaxMessages = 10,
    WaitTimeoutSeconds = 5
});
logger.LogInformation("Peeked {Count} messages (not consumed)", peeked.Messages.Count);
foreach (var msg in peeked.Messages)
{
    logger.LogInformation("  [Peek] {Body}", Encoding.UTF8.GetString(msg.Body.Span));
}

// Receive — now messages are consumed
var received = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.peek",
    MaxMessages = 10,
    WaitTimeoutSeconds = 5,
    AutoAck = true
});
logger.LogInformation("Received {Count} messages (consumed)", received.Messages.Count);

await host.RunAsync();
