// KubeMQ Aspire — Queries: Handle Query
//
// Subscribes to incoming queries and responds with data.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queries;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ — waiting for queries on queries.send");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription { Channel = "queries.send" };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(query.Body.Span);
            logger.LogInformation("Received query: {Body}", body);

            // Respond with data
            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes("{\"name\":\"John\",\"role\":\"admin\"}")
            }, stoppingToken);

            logger.LogInformation("Responded with user info");
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
