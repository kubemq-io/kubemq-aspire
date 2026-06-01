// KubeMQ Aspire — PubSub: Cancel Subscription
//
// Subscribes, receives N messages, then explicitly cancels the subscription.
// Demonstrates using CancellationTokenSource to control subscription lifetime.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

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
const int maxMessages = 3;

// Subscriber — cancels after receiving 3 messages
_ = Task.Run(async () =>
{
    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var count = 0;

        var subscription = new EventsSubscription { Channel = "events.cancel" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, cts.Token))
        {
            count++;
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("Received [{Count}/{Max}]: {Body}", count, maxMessages, body);

            if (count >= maxMessages)
            {
                logger.LogInformation("Received {Max} messages — cancelling subscription", maxMessages);
                cts.Cancel();
            }
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Subscription cancelled gracefully");
    }
}, stoppingToken);

// Publisher — sends more than maxMessages
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 10; i++)
        {
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.cancel",
                Body = Encoding.UTF8.GetBytes($"Event #{i}")
            }, stoppingToken);
            await Task.Delay(500, stoppingToken);
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
