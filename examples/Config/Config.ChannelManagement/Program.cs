// KubeMQ Aspire — Config: Channel Management
//
// Creates, lists, and deletes channels of different types.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ");

// Create channels of different types
await client.CreateEventsChannelAsync("managed.events.test");
await client.CreateEventsStoreChannelAsync("managed.store.test");
await client.CreateQueuesChannelAsync("managed.queues.test");
await client.CreateCommandsChannelAsync("managed.commands.test");
await client.CreateQueriesChannelAsync("managed.queries.test");
logger.LogInformation("Created 5 channels");

// List events channels
var eventsChannels = await client.ListEventsChannelsAsync("managed.*");
logger.LogInformation("Events channels ({Count}):", eventsChannels.Count);
foreach (var ch in eventsChannels)
{
    logger.LogInformation("  -> {Name}", ch.Name);
}

// List all queues channels
var queuesChannels = await client.ListQueuesChannelsAsync();
logger.LogInformation("Queues channels ({Count}):", queuesChannels.Count);
foreach (var ch in queuesChannels)
{
    logger.LogInformation("  -> {Name}", ch.Name);
}

// Delete the test channels
await client.DeleteEventsChannelAsync("managed.events.test");
await client.DeleteQueuesChannelAsync("managed.queues.test");
logger.LogInformation("Deleted test channels");

await host.RunAsync();
