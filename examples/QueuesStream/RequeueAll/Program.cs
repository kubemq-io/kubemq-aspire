// KubeMQ Aspire — QueuesStream: Requeue All
//
// Requeues all messages to a different channel using batch-level settlement.

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

for (var i = 1; i <= 3; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.requeueall",
        Body = Encoding.UTF8.GetBytes($"Requeue #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.requeueall",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    logger.LogInformation("Requeuing {Count} messages to stream.requeueall.redirect", batch.Messages.Count);
    await batch.ReQueueAllAsync("stream.requeueall.redirect");
    logger.LogInformation("All messages requeued to redirect channel");
}

await host.RunAsync();
