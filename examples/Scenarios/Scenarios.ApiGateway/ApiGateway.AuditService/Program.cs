// KubeMQ Aspire — Scenario: API Gateway — Audit Service

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
logger.LogInformation("Audit Service started — listening on events.audit");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new EventsSubscription { Channel = "events.audit" };
        await foreach (var ev in client.SubscribeToEventsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(ev.Body.Span);
            logger.LogInformation("[Audit] {Timestamp}: {Body}", DateTime.UtcNow, body);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
