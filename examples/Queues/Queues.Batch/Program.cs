// KubeMQ Aspire — Queues: Batch Send and Receive
//
// Sends multiple messages in a batch and receives them all at once.

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

// Batch send 5 messages
var messages = Enumerable.Range(1, 5).Select(i => new QueueMessage
{
    Channel = "queues.batch",
    Body = Encoding.UTF8.GetBytes($"Batch message #{i}")
}).ToList();

var batchResult = await client.SendQueueMessagesAsync(messages);
logger.LogInformation("Batch sent: IsError={IsError}", batchResult.IsError);

// Receive all messages in one poll
var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.batch",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = true
});

logger.LogInformation("Received {Count} messages", response.Messages.Count);
foreach (var msg in response.Messages)
{
    var body = Encoding.UTF8.GetString(msg.Body.Span);
    logger.LogInformation("  -> {Body}", body);
}

await host.RunAsync();
