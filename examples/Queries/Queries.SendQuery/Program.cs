// KubeMQ Aspire — Queries: Send Query
//
// Sends a query and receives a response with data.

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

// NOTE: Run Queries.HandleQuery in parallel
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);
        var response = await client.SendQueryAsync(new QueryMessage
        {
            Channel = "queries.send",
            Body = Encoding.UTF8.GetBytes("get-user-info"),
            TimeoutInSeconds = 10
        }, stoppingToken);

        if (response.Executed)
        {
            var data = Encoding.UTF8.GetString(response.Body.Span);
            logger.LogInformation("Query response: {Data}, CacheHit={CacheHit}",
                data, response.CacheHit);
        }
        else
        {
            logger.LogWarning("Query failed: {Error}", response.Error);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
