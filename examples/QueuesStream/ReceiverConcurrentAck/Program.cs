// KubeMQ Aspire — QueuesStream: Concurrent Acknowledgement
//
// Demonstrates processing and acknowledging messages concurrently.

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

for (var i = 1; i <= 10; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.concurrent",
        Body = Encoding.UTF8.GetBytes($"Concurrent #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.concurrent",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    // Process and ack all messages concurrently
    var tasks = batch.Messages.Select(async msg =>
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("Processing: {Body}", body);
        await Task.Delay(100); // simulate work
        await msg.AckAsync();
        logger.LogInformation("Acked: {Body}", body);
    });

    await Task.WhenAll(tasks);
    logger.LogInformation("All {Count} messages processed concurrently", batch.Messages.Count);
}

await host.RunAsync();
