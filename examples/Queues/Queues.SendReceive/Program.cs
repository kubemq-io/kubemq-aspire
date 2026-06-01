// KubeMQ Aspire — Queues: Send and Receive
//
// Sends a queue message and receives it with acknowledgment.
// Queue messages are point-to-point — exactly one consumer gets each message.

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

// Send a queue message
var sendResult = await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.sendreceive",
    Body = Encoding.UTF8.GetBytes("Order #1234 — process this"),
    Tags = new Dictionary<string, string> { ["priority"] = "high" }
});
logger.LogInformation("Sent message: {MessageId}, IsError={IsError}", sendResult.MessageId, sendResult.IsError);

// Receive using the poll API
var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "queues.sendreceive",
    MaxMessages = 1,
    WaitTimeoutSeconds = 10,
    AutoAck = true // automatically acknowledge on receive
});

if (response.HasMessages)
{
    foreach (var msg in response.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("Received: {Body}, MessageId={MessageId}", body, msg.MessageId);
    }
}
else
{
    logger.LogWarning("No messages received");
}

await host.RunAsync();
