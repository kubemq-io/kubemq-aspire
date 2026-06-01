// KubeMQ Aspire — Scenario: API Gateway
//
// HTTP gateway that routes requests to backend services via KubeMQ Queries.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;
using KubeMQ.Sdk.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var app = builder.Build();
var client = app.Services.GetRequiredService<IKubeMQClient>();
await client.ConnectAsync();

app.MapGet("/users/{id}", async (string id) =>
{
    await client.SendEventAsync(new EventMessage
    {
        Channel = "events.audit",
        Body = Encoding.UTF8.GetBytes($"GET /users/{id}")
    });

    var response = await client.SendQueryAsync(new QueryMessage
    {
        Channel = "queries.users",
        Body = Encoding.UTF8.GetBytes(id),
        TimeoutInSeconds = 5,
        CacheKey = $"user:{id}",
        CacheTtlSeconds = 30
    });

    return response.Executed
        ? Results.Ok(Encoding.UTF8.GetString(response.Body.Span))
        : Results.StatusCode(503);
});

app.MapGet("/products/{id}", async (string id) =>
{
    await client.SendEventAsync(new EventMessage
    {
        Channel = "events.audit",
        Body = Encoding.UTF8.GetBytes($"GET /products/{id}")
    });

    var response = await client.SendQueryAsync(new QueryMessage
    {
        Channel = "queries.products",
        Body = Encoding.UTF8.GetBytes(id),
        TimeoutInSeconds = 5
    });

    return response.Executed
        ? Results.Ok(Encoding.UTF8.GetString(response.Body.Span))
        : Results.StatusCode(503);
});

app.MapGet("/", () => "API Gateway — /users/{id}, /products/{id}");
app.Run();
