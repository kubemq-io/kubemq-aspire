// KubeMQ Aspire — Queries: Cached Response
//
// Sends queries with cache TTL — subsequent identical queries hit the cache.

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
logger.LogInformation("Connected to KubeMQ");

var stoppingToken = lifetime.ApplicationStopping;

// Handler
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription { Channel = "queries.cached" };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Handler] Processing query (this should only happen once)");
            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes("{\"price\":42.99,\"timestamp\":\"" + DateTime.UtcNow + "\"}")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Sender — sends the same query twice; second should be cached
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);

        // First query — handler processes it
        var response1 = await client.SendQueryAsync(new QueryMessage
        {
            Channel = "queries.cached",
            Body = Encoding.UTF8.GetBytes("get-price"),
            TimeoutInSeconds = 10,
            CacheKey = "price-query",
            CacheTtlSeconds = 60
        }, stoppingToken);
        logger.LogInformation("Query 1: CacheHit={CacheHit}, Data={Data}",
            response1.CacheHit, Encoding.UTF8.GetString(response1.Body.Span));

        await Task.Delay(1000, stoppingToken);

        // Second query — should be served from cache
        var response2 = await client.SendQueryAsync(new QueryMessage
        {
            Channel = "queries.cached",
            Body = Encoding.UTF8.GetBytes("get-price"),
            TimeoutInSeconds = 10,
            CacheKey = "price-query",
            CacheTtlSeconds = 60
        }, stoppingToken);
        logger.LogInformation("Query 2: CacheHit={CacheHit}, Data={Data}",
            response2.CacheHit, Encoding.UTF8.GetString(response2.Body.Span));
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
