// KubeMQ Aspire — Queues: Expiration Policy
//
// Messages expire after a TTL if not consumed in time.

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

// Send a message that expires in 3 seconds
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.expiration",
    Body = Encoding.UTF8.GetBytes("Expires in 3 seconds!"),
    ExpirationSeconds = 3
});
logger.LogInformation("Sent message with 3s TTL at {Time}", DateTime.UtcNow);

// Wait past expiration
logger.LogInformation("Waiting 4 seconds for message to expire...");
await Task.Delay(4000);

// Try to receive — message should be gone
var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.expiration",
    MaxMessages = 1,
    WaitTimeoutSeconds = 2,
    AutoAck = true
});
logger.LogInformation("After expiration: {Count} messages (expected 0)", response.Messages.Count);

await host.RunAsync();
