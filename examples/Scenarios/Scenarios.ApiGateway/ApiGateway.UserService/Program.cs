// KubeMQ Aspire — Scenario: API Gateway — User Service

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
logger.LogInformation("User Service started");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription { Channel = "queries.users" };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            var userId = Encoding.UTF8.GetString(query.Body.Span);
            logger.LogInformation("[UserService] Query for user: {Id}", userId);

            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes($"{{\"id\":\"{userId}\",\"name\":\"User {userId}\"}}")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
