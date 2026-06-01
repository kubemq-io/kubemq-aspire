// KubeMQ Aspire — Scenario: Real-Time Chat — Server
//
// REST endpoints for sending messages. Uses PubSub for live delivery
// and EventsStore for message persistence.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;
using KubeMQ.Sdk.EventsStore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var app = builder.Build();
var client = app.Services.GetRequiredService<IKubeMQClient>();
await client.ConnectAsync();

app.MapPost("/chat/{room}", async (string room, HttpContext ctx) =>
{
    var message = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

    // Broadcast live
    await client.SendEventAsync(new EventMessage
    {
        Channel = $"events.chat.{room}",
        Body = Encoding.UTF8.GetBytes(message)
    });

    // Persist for history
    await client.SendEventStoreAsync(new EventStoreMessage
    {
        Channel = $"store.chat.{room}",
        Body = Encoding.UTF8.GetBytes(message)
    });

    return Results.Ok(new { room, status = "sent" });
});

app.MapGet("/", () => "Chat Server — POST /chat/{room}");
app.Run();
