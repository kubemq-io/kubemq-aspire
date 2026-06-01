// KubeMQ Aspire — QueuesStream: Per-Message Settlement
//
// Demonstrates ack/nack/requeue decisions on a per-message basis.

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

// Send 3 messages with different processing outcomes
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "stream.permessage",
    Body = Encoding.UTF8.GetBytes("good-message"),
    Tags = new Dictionary<string, string> { ["action"] = "ack" }
});
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "stream.permessage",
    Body = Encoding.UTF8.GetBytes("bad-message"),
    Tags = new Dictionary<string, string> { ["action"] = "nack" }
});
await client.SendQueueMessageAsync(new QueueMessage
{
    Channel = "stream.permessage",
    Body = Encoding.UTF8.GetBytes("redirect-message"),
    Tags = new Dictionary<string, string> { ["action"] = "requeue" }
});

await using var receiver = await client.CreateQueueDownstreamReceiverAsync();

var batch = await receiver.PollAsync(new QueuePollRequest
{
    Channel = "stream.permessage",
    MaxMessages = 10,
    WaitTimeoutSeconds = 10,
    AutoAck = false
});

if (batch.HasMessages)
{
    foreach (var msg in batch.Messages)
    {
        var action = msg.Tags?["action"] ?? "ack";
        var body = Encoding.UTF8.GetString(msg.Body.Span);

        switch (action)
        {
            case "ack":
                await msg.AckAsync();
                logger.LogInformation("[ACK] {Body}", body);
                break;
            case "nack":
                await msg.NackAsync();
                logger.LogInformation("[NACK] {Body} — returned to queue", body);
                break;
            case "requeue":
                await msg.ReQueueAsync("stream.permessage.redirect");
                logger.LogInformation("[REQUEUE] {Body} — sent to redirect channel", body);
                break;
        }
    }
}

await host.RunAsync();
