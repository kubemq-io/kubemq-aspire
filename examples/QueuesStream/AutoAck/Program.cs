// KubeMQ Aspire — QueuesStream: Auto Ack
//
// Uses AutoAck=true — messages are acknowledged automatically on receive.

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

for (var i = 1; i <= 5; i++)
{
    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "stream.autoack",
        Body = Encoding.UTF8.GetBytes($"Auto-ack #{i}")
    });
}

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.autoack",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = true // automatically acknowledged
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var body = Encoding.UTF8.GetString(msg.Body.Span);
        logger.LogInformation("[AutoAck] {Body} — already acknowledged", body);
    }
}

await host.RunAsync();
