// KubeMQ Aspire — QueuesStream: Stream Receive
//
// Receives messages via a persistent downstream receiver stream.

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

// Send test messages
for (var i = 1; i <= 5; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.receive",
        Body = Encoding.UTF8.GetBytes($"Stream receive #{i}")
    });
}

// Create downstream receiver and poll
await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.receive",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("[StreamRecv] {Body}", body);
        await msg.AckAsync();
    }
    logger.LogInformation("Received and acked {Count} messages", batch.Messages.Count);
}

await host.RunAsync();
