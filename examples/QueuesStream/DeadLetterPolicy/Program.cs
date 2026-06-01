// KubeMQ Aspire — QueuesStream: Dead Letter Policy
//
// Stream receive with messages that exceed max delivery attempts.

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

await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "stream.dlpolicy",
    Body = Encoding.UTF8.GetBytes("Poison stream message"),
    MaxReceiveCount = 2,
    MaxReceiveQueue = "stream.dlpolicy.dead"
});

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

// Nack the message twice to trigger DLQ
for (var attempt = 1; attempt <= 2; attempt++)
{
    var batch = await receiver.PollAsync(new QueuePollRequest
    {
        Channel = "stream.dlpolicy",
        MaxMessages = 1,
        WaitTimeoutSeconds = 5,
        AutoAck = false
    });

    if (batch.HasMessages)
    {
        logger.LogInformation("Attempt {Attempt}: nacking poison message", attempt);
        await batch.NackAllAsync();
    }
}

await Task.Delay(1000);
var dlq = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
{
    Channel = "stream.dlpolicy.dead",
    MaxMessages = 1,
    WaitTimeoutSeconds = 5,
    AutoAck = true
});

if (dlq.HasMessages)
{
    var body = Encoding.UTF8.GetString(dlq.Messages[0].Body.Span);
    logger.LogInformation("[DLQ] {Body}", body);
}

await host.RunAsync();
