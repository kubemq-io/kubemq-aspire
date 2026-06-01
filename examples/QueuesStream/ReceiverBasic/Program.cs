// KubeMQ Aspire — QueuesStream: Receiver Basic
//
// Basic downstream receiver with manual ack for each message.

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

// Send test data
for (var i = 1; i <= 3; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.basic",
        Body = Encoding.UTF8.GetBytes($"Basic message #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.basic",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("Processing: {Body}", body);
        // Manual acknowledgment after processing
        await msg.AckAsync();
        logger.LogInformation("  -> Acknowledged");
    }
}

await host.RunAsync();
