// KubeMQ Aspire — Scenario: Order Processing — API
//
// Receives HTTP orders and sends them to a queue for processing.

using System.Text;
using System.Text.Json;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Queues;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var app = builder.Build();
var client = app.Services.GetRequiredService<IKubeMQClient>();
await client.ConnectAsync();

app.MapPost("/orders", async (HttpContext ctx) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var orderId = Guid.NewGuid().ToString("N")[..8];

    await client.SendQueueMessageAsync(new QueueMessage
    {
        Channel = "queues.orders",
        Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { orderId, body })),
        Tags = new Dictionary<string, string> { ["orderId"] = orderId }
    });

    return Results.Ok(new { orderId, status = "queued" });
});

app.MapGet("/", () => "Order Processing API — POST /orders to create");
app.Run();
