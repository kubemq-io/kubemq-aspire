// KubeMQ Aspire — QueuesStream: Stream Send
//
// High-throughput batch send via upstream stream API.

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

// Send batch via upstream stream
var messages = Enumerable.Range(1, 20).Select(i => new QueueMessage
{
    Channel = "stream.send",
    Body = Encoding.UTF8.GetBytes($"Stream message #{i}")
}).ToList();

var result = await client.SendQueueMessagesUpstreamAsync(messages);
logger.LogInformation("Upstream sent {Count} messages, IsError={IsError}", messages.Count, result.IsError);

await host.RunAsync();
