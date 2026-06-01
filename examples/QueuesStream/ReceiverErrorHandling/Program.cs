// KubeMQ Aspire — QueuesStream: Error Handling
//
// Demonstrates error handling in downstream receiver — nack on failure.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;
using KubeMQ.Sdk.Exceptions;

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
        Channel = "stream.errorhandling",
        Body = Encoding.UTF8.GetBytes(i == 2 ? "BAD_DATA" : $"Good message #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.errorhandling",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        try
        {
            if (body == "BAD_DATA")
                throw new InvalidOperationException("Cannot process BAD_DATA");

            logger.LogInformation("Processed: {Body}", body);
            await msg.AckAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message — nacking");
            await msg.NackAsync();
        }
    }
}

await host.RunAsync();
