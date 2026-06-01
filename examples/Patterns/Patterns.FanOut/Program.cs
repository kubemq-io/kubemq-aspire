// KubeMQ Aspire — Patterns: Fan-Out
//
// One publisher, multiple subscribers — all receive every message.

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
var stoppingToken = lifetime.ApplicationStopping;

// 3 subscribers — all receive every message
for (var s = 1; s <= 3; s++)
{
    var subId = s;
    _ = Task.Run(async () =>
    {
        try
        {
            var subscription = new EventsSubscription { Channel = "events.fanout" };
            await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
            {
                logger.LogInformation("[Sub-{Id}] {Body}", subId, Encoding.UTF8.GetString(ev.Body.Span));
            }
        }
        catch (OperationCanceledException) { }
    }, stoppingToken);
}

// Publisher
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.fanout",
                Body = Encoding.UTF8.GetBytes($"Broadcast #{i}")
            }, stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
