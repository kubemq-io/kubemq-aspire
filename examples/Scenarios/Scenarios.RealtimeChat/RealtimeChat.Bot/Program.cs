// KubeMQ Aspire — Scenario: Real-Time Chat — Bot
//
// Auto-reply bot that listens for messages and responds.

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
logger.LogInformation("Chat bot started — listening on events.chat.*");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.chat.*" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Bot] Heard: {Body} on {Channel}", body, ev.Channel);

            // Auto-reply
            await client.SendEventAsync(new EventMessage
            {
                Channel = ev.Channel,
                Body = Encoding.UTF8.GetBytes($"Bot reply to: {body}")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
