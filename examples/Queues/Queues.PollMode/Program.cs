// KubeMQ Aspire — Queues: Poll Mode
//
// Continuous polling loop for new messages.

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

// Publisher — sends messages periodically
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
                Channel = "queues.poll",
                Body = Encoding.UTF8.GetBytes($"Poll message #{i}")
            }, stoppingToken);
            await Task.Delay(2000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

// Consumer — continuous poll loop
_ = Task.Run(async () =>
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await client.ReceiveQueueMessagesAsync(new QueuePollRequest
            {
                Channel = "queues.poll",
                MaxMessages = 5,
                WaitTimeoutSeconds = 5,
                AutoAck = true
            }, stoppingToken);

            if (response.HasMessages)
            {
                foreach (var msg in response.Messages)
                {
                    var body = Encoding.UTF8.GetString(msg.Body.Span);
                    logger.LogInformation("[Poll] {Body}", body);
                }
            }
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
