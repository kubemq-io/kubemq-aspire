// KubeMQ Aspire — Queues: Ack and Reject
//
// Receives a message and demonstrates both acknowledge and reject (nack).
// Rejected messages are returned to the queue for redelivery.

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

// Send two messages
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.ackreject",
    Body = Encoding.UTF8.GetBytes("Message to ACK")
});
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "queues.ackreject",
    Body = Encoding.UTF8.GetBytes("Message to REJECT")
});
logger.LogInformation("Sent 2 messages");

// Receive with manual settlement via downstream receiver
await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "queues.ackreject",
    MaxMessages = 2,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        if (body.Contains("ACK"))
        {
            await msg.AckAsync();
            logger.LogInformation("ACK: {Body}", body);
        }
        else
        {
            await msg.NackAsync();
            logger.LogInformation("NACK (rejected): {Body}", body);
        }
    }
}

await host.RunAsync();
