// KubeMQ Aspire — Queries: Consumer Group
//
// Multiple query handlers in a group — only one handles each query.

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

// Handler A
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription
        {
            Channel = "queries.group",
            Group = "handlers"
        };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Handler-A] Query: {Body}", Encoding.UTF8.GetString(query.Body.Span));
            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes("{\"handler\":\"A\"}")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Handler B
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new QueriesSubscription
        {
            Channel = "queries.group",
            Group = "handlers"
        };
        await foreach (var query in client.SubscribeToQueriesAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Handler-B] Query: {Body}", Encoding.UTF8.GetString(query.Body.Span));
            await client.SendQueryResponseAsync(new QueryResponse
            {
                RequestId = query.RequestId,
                ReplyChannel = query.ReplyChannel!,
                Executed = true,
                Body = Encoding.UTF8.GetBytes("{\"handler\":\"B\"}")
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Sender
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            var response = await client.SendQueryAsync(new QueryMessage
            {
                Channel = "queries.group",
                Body = Encoding.UTF8.GetBytes($"Query #{i}"),
                TimeoutInSeconds = 10
            }, stoppingToken);
            var data = Encoding.UTF8.GetString(response.Body.Span);
            logger.LogInformation("Query #{I} answered by: {Data}", i, data);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
