// KubeMQ Aspire — QueuesStream: Poll Mode
//
// Continuous downstream polling loop with the stream receiver.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ");

var stoppingToken = lifetime.ApplicationStopping;

// Publisher
_ = Task.Run(async () =>
{
    try
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            i++;
            await client.SendQueueMessageAsync(new QueueMessage
            {
                Channel = "stream.poll",
                Body = Encoding.UTF8.GetBytes($"Poll stream #{i}")
            }, stoppingToken);
            await Task.Delay(2000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Downstream poll loop
_ = Task.Run(async () =>
{
    try
    {
        await using var receiver = await client.CreateQueueDownstreamReceiverAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await receiver.PollAsync(new QueuePollRequest
            {
                Channel = "stream.poll",
                MaxMessages = 5,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);

            if (batch.HasMessages)
            {
                foreach (var msg in batch.Messages)
                {
                    var body = Encoding.UTF8.GetString(msg.Body.Span);
                    logger.LogInformation("[StreamPoll] {Body}", body);
                }
            }
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
