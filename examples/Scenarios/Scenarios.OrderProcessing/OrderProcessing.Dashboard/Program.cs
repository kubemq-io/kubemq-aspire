// KubeMQ Aspire — Scenario: Order Processing — Dashboard
//
// Web API that queries order status via Commands.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var app = builder.Build();
var client = app.Services.GetRequiredService<IKubeMQClient>();
await client.ConnectAsync();

app.MapGet("/status/{orderId}", async (string orderId) =>
{
    try
    {
        var response = await client.SendQueryAsync(new QueryMessage
        {
            Channel = "queries.order-status",
            Body = Encoding.UTF8.GetBytes(orderId),
            TimeoutInSeconds = 5
        });
        if (response.Executed)
            return Results.Ok(new { orderId, status = Encoding.UTF8.GetString(response.Body.Span) });
        return Results.NotFound(new { orderId, error = response.Error });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.MapGet("/", () => "Dashboard — GET /status/{orderId}");
app.Run();
