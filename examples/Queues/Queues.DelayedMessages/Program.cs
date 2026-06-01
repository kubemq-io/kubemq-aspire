// KubeMQ Aspire — Queues: Delayed Messages
//
// Sends messages with a delivery delay — they become visible after the delay.

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

// Send a message with 5-second delay
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.delayed",
    Body = Encoding.UTF8.GetBytes("Delayed message — visible after 5 seconds"),
    DelaySeconds = 5
});
logger.LogInformation("Sent delayed message (5s delay) at {Time}", DateTime.UtcNow);

// Try to receive immediately — should get nothing
var immediate = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.delayed",
    MaxMessages = 1,
    WaitTimeoutSeconds = 2,
    AutoAck = true
});
logger.LogInformation("Immediate poll: {Count} messages", immediate.Messages.Count);

// Wait for delay, then receive
logger.LogInformation("Waiting for delay to expire...");
await Task.Delay(5000);

var delayed = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.delayed",
    MaxMessages = 1,
    WaitTimeoutSeconds = 5,
    AutoAck = true
});
if (delayed.HasMessages)
{
    var body = Encoding.UTF8.GetString(delayed.Messages[0].Body.Span);
    logger.LogInformation("After delay: {Body}", body);
}

await host.RunAsync();
