// KubeMQ Aspire — Config: Purge Queue
//
// Purges all messages from a queue without deleting the queue itself.

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

// Send messages to queue
for (var i = 1; i <= 5; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "queues.purge",
        Body = Encoding.UTF8.GetBytes($"Purge test #{i}")
    });
}
logger.LogInformation("Sent 5 messages to queues.purge");

// Purge all messages
var result = await client.PurgeQueueAsync("queues.purge");
logger.LogInformation("Purged queue: AffectedMessages={Count}, IsError={IsError}",
    result.AffectedMessages, result.IsError);

// Verify queue is empty
var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.purge",
    MaxMessages = 10,
    WaitTimeoutSeconds = 2,
    AutoAck = true
});
logger.LogInformation("After purge: {Count} messages (expected 0)", response.Messages.Count);

await host.RunAsync();
