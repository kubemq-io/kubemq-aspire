// KubeMQ Aspire — Scenario: Real-Time Chat — Worker
//
// Persists messages and handles history retrieval queries.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.EventsStore;
using KubeMQ.Sdk.Queries;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Chat worker started");

var stoppingToken = lifetime.ApplicationStopping;

// Handle history queries
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription { Channel = "queries.chat-history" };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[History] Query for: {Body}", Encoding.UTF8.GetString(query.Body.Span));
            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes("[{\"msg\":\"Hello\"},{\"msg\":\"World\"}]")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
